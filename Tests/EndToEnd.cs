namespace Tests
{
	using Autofac.Core.Lifetime;
	using Moq;
	using OctoHook;
	using Autofac;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Xunit;
	using OctoHook.Controllers;
	using Octokit;
	using System.IO;
	using Octokit.Internal;
	using Newtonsoft.Json;
	using Octokit.Events;
	using System.Net.Http;
	using Newtonsoft.Json.Linq;
	using OctoHook.WebHooks;

	public class EndToEnd
	{
		static readonly Credentials credentials = new Credentials(File.ReadAllText(@"..\..\Token").Trim());

		[Fact]
		public async Task when_processing_issues_request_then_succeeds()
		{
			var work = new Mock<IWorkQueue>();
			work.Setup(x => x.Queue(It.IsAny<Action>(), It.IsAny<string>()))
				.Callback<Action, string>((a, s) => a());

			var container = ContainerConfiguration.Configure(work.Object);
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
	}
}
