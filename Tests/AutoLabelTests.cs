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

	public class AutoLabelTests
	{
		static readonly Credentials credentials = new Credentials(File.ReadAllText(@"..\..\Token").Trim());

		[Fact]
		public async Task when_processing_issue_with_lower_case_label_then_automatically_adds_labels()
		{
			var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));
			var repository = await github.Repository.Get("kzu", "sandbox");
			var user = await github.User.Current();

			var issue = await github.Issue.Create(
				"kzu", "sandbox", new NewIssue("Auto-labeling to stories ~story"));

			var labeler = new AutoLabel(github);

			labeler.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = issue,
				Repository = repository,
				Sender = user,
			});

			var updated = await github.Issue.Get("kzu", "sandbox", issue.Number);

			Assert.Equal("Auto-labeling to stories", updated.Title);
			Assert.True(updated.Labels.Any(l => l.Name == "Story"));

			await github.Issue.Update("kzu", "sandbox", issue.Number, new IssueUpdate { State = ItemState.Closed });
		}

		[Fact]
		public async Task when_processing_issue_with_declared_mid_title_then_does_not_apply_label()
		{
			var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));
			var repository = await github.Repository.Get("kzu", "sandbox");
			var user = await github.User.Current();

			var issue = await github.Issue.Create(
				"kzu", "sandbox", new NewIssue("Auto-labeling to ~foo in the middle doesn't work"));

			var labeler = new AutoLabel(github);

			labeler.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = issue,
				Repository = repository,
				Sender = user,
			});

			var updated = await github.Issue.Get("kzu", "sandbox", issue.Number);

			Assert.Equal("Auto-labeling to stories", updated.Title);
			Assert.False(updated.Labels.Any(l => l.Name == "foo"));

			await github.Issue.Update("kzu", "sandbox", issue.Number, new IssueUpdate { State = ItemState.Closed });
		}

		[Fact]
		public async Task when_processing_issue_with_declared_plus_label_then_applies_it_with_plus()
		{
			var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));
			var repository = await github.Repository.Get("kzu", "sandbox");
			var user = await github.User.Current();

			var issue = await github.Issue.Create(
				"kzu", "sandbox", new NewIssue("Auto-labeling to +doc"));

			var labeler = new AutoLabel(github);

			labeler.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = issue,
				Repository = repository,
				Sender = user,
			});

			var updated = await github.Issue.Get("kzu", "sandbox", issue.Number);

			Assert.Equal("Auto-labeling to", updated.Title);
			Assert.True(updated.Labels.Any(l => l.Name == "+Doc"));

			await github.Issue.Update("kzu", "sandbox", issue.Number, new IssueUpdate { State = ItemState.Closed });
		}


		[Fact]
		public async Task when_processing_issue_with_undeclared_plus_label_then_applies_it_without_plus()
		{
			var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));

			var repository = await github.Repository.Get("kzu", "sandbox");
			var user = await github.User.Current();

			var issue = await github.Issue.Create(
				"kzu", "sandbox", new NewIssue("Auto-labeling to +foo"));

			var labeler = new AutoLabel(github);

			try
			{
				await github.Issue.Labels.Delete("kzu", "sandbox", "foo");
			}
			catch { }

			labeler.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = issue,
				Repository = repository,
				Sender = user,
			});

			var updated = await github.Issue.Get("kzu", "sandbox", issue.Number);

			Assert.Equal("Auto-labeling to", updated.Title);
			Assert.True(updated.Labels.Any(l => l.Name == "foo"));

			await github.Issue.Update("kzu", "sandbox", issue.Number, new IssueUpdate { State = ItemState.Closed });

			try
			{
				await github.Issue.Labels.Delete("kzu", "sandbox", "foo");
			}
			catch { }
		}
	}
}
