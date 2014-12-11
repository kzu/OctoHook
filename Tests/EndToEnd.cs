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
	using OctoHook.CommonComposition;
    using OctoHook.Web;
	using Octokit;
	using Octokit.Events;
	using Octokit.Internal;
	using System;
	using System.IO;
	using System.Net.Http;
	using System.Threading.Tasks;
	using Xunit;
    using System.Diagnostics;

	public class EndToEnd
	{
		static readonly Credentials credentials = TestCredentials.Create();

		[Fact]
		public void when_retrieving_scoped_instances_then_succeeds()
		{
			var container = ContainerConfiguration.Configure(
                credentials.GetToken(),
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
		}

		[Fact]
		public async Task when_processing_issues_request_then_succeeds()
		{
			var work = new Mock<IJobQueue>();
			work.Setup(x => x.Queue(It.IsAny<Func<Task>>()))
				.Callback<Func<Task>>(a => a().Wait());

			var container = ContainerConfiguration.Configure(
                credentials.GetToken(),
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

            var json = File.ReadAllText(@"..\..\test.json");

			var request = new HttpRequestMessage(HttpMethod.Post, "http://octohook.azurewebsites.net/github")
			{
				Headers = 
				{
					{ "X-GitHub-Event", "issues" }
				}
			};

            var controller = new OctoController(credentials.GetToken(), SourceLevels.Critical);
			controller.Post(request, JObject.Parse(json));
		}

		[Component(IsSingleton = true)]
		public class PerApp : IApp { }

		public interface IApp { }

		[Component]
		public class PerRequest : IJob { }

		public interface IJob { }
	}
}
