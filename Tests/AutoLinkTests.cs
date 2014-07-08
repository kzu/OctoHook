namespace Tests
{
	using Newtonsoft.Json.Linq;
	using OctoHook.WebHooks;
	using Octokit;
	using Octokit.Events;
	using Octokit.Internal;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using Xunit;

	public class AutoLinkTests
	{
		static readonly Credentials credentials = new Credentials(File.ReadAllText(@"..\..\Token").Trim());

		[Fact]
		public async Task when_processing_issue_without_link_then_automatically_links()
		{
			var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));
			var repository = await github.Repository.Get("kzu", "sandbox");
			var user = await github.User.Current();

			var story = await github.Issue.Create(
				"kzu", "sandbox", new NewIssue("[hook] Auto-linking to stories")
				{
					Labels = { "Story" },
				});

			var task = await github.Issue.Create(
				"kzu", "sandbox", new NewIssue("[hook] Task about auto-linking")
				{
					Labels = { "Task" },
				});

			var linker = new AutoLink(github);

			linker.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = user,
			});

			var updated = await github.Issue.Get("kzu", "sandbox", task.Number);

			Assert.True(updated.Body.Contains("#" + story.Number));

			await github.Issue.Update("kzu", "sandbox", story.Number, new IssueUpdate { State = ItemState.Closed });
			await github.Issue.Update("kzu", "sandbox", task.Number, new IssueUpdate { State = ItemState.Closed });
		}

		[Fact]
		public async Task when_processing_issue_then_automatically_links_with_closed_story()
		{
			var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));
			var repository = await github.Repository.Get("kzu", "sandbox");
			var user = await github.User.Current();

			var story = await github.Issue.Create(
				"kzu", "sandbox", new NewIssue("[hook] Auto-linking to stories")
				{
					Labels = { "Story" },
				});
			await github.Issue.Update("kzu", "sandbox", story.Number, new IssueUpdate { State = ItemState.Closed });

			var task = await github.Issue.Create(
				"kzu", "sandbox", new NewIssue("[hook] Task about auto-linking")
				{
					Labels = { "Task" },
				});

			var linker = new AutoLink(github);

			linker.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = user,
			});

			var updated = await github.Issue.Get("kzu", "sandbox", task.Number);

			Assert.True(updated.Body.Contains("#" + story.Number));
		}
	}
}
