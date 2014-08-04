namespace Tests
{
    using Autofac;
    using Autofac.Builder;
    using Autofac.Core;
    using Autofac.Features.Scanning;
	using Autofac.Core.Lifetime;
	using Moq;
	using Newtonsoft.Json;
	using System.Linq;
	using Newtonsoft.Json.Linq;
	using OctoHook;
	using OctoHook.Controllers;
	using OctoHook.CommonComposition;
	using Octokit;
	using Octokit.Events;
	using Octokit.Internal;
	using System;
	using System.IO;
	using System.Net.Http;
	using System.Threading.Tasks;
	using Xunit;

	public class EndToEnd
	{
		static readonly Credentials credentials = new Credentials(File.ReadAllText(@"..\..\Token").Trim());

		[Fact]
		public void when_retrieving_scoped_instances_then_succeeds()
		{
			var container = ContainerConfiguration.Configure(
				typeof(AutoAssign).Assembly, 
				typeof(AutoClose).Assembly, 
				typeof(AutoLabel).Assembly, 
				typeof(AutoLink).Assembly, 
				typeof(OctoIssuerJob).Assembly
			);

			var lifetime = container.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag);

			var queue1 = container.Resolve<IJobQueue>();
			var queue2 = container.Resolve<IJobQueue>();

			Assert.Same(queue1, queue2);

			Assert.NotNull(lifetime.Resolve<OctoIssuerJob>());

			Assert.NotNull(lifetime.Resolve<GitHubController>());
		}

		[Fact]
		public async Task when_processing_issues_request_then_succeeds()
		{
			var work = new Mock<IJobQueue>();
			work.Setup(x => x.Queue(It.IsAny<Func<Task>>()))
				.Callback<Func<Task>>(a => a().Wait());

			var container = ContainerConfiguration.Configure(
				typeof(AutoAssign).Assembly, 
				typeof(AutoClose).Assembly, 
				typeof(AutoLabel).Assembly, 
				typeof(AutoLink).Assembly, 
				typeof(OctoIssuerJob).Assembly
			);
			var lifetime = container.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag);

			var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));
			var repository = await github.Repository.Get("kzu", "sandbox");
			var user = await github.User.Current();

			var issue = await github.Issue.Create(
				"kzu", "sandbox", new NewIssue("Auto-labeling to stories and assigning to kzu +story :kzu"));

			var json = JsonConvert.SerializeObject(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = issue,
				Repository = repository,
				Sender = user,
			});

			var request = new HttpRequestMessage(HttpMethod.Post, "github")
			{
				Headers = 
				{
					{ "X-GitHub-Event", "issues" }
				}
			};

			var controller = lifetime.Resolve<GitHubController>();
			controller.Post(request, JObject.Parse(json));
		}

		//[Fact]
		//public void when_processing_non_singletons_then_can_set_as_instance_per_request()
		//{
		//	var builder = new ContainerBuilder();

		//}

		[Component(IsSingleton = true)]
		public class PerApp : IApp { }

		public interface IApp { }

		[Component]
		public class PerRequest : IJob { }

		public interface IJob { }
	}
}
