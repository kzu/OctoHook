namespace Tests
{
	using Moq;
	using Newtonsoft.Json.Linq;
	using OctoHook;
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
		static readonly Credentials credentials = TestCredentials.Create();

		static readonly Repository repository = new Repository
			{
				Owner = new User
				{
					Login = "kzu"
				},
				Name = "autolink",
			};

		[Fact]
		public void when_processing_issue_without_link_then_automatically_links()
		{
			var github = new Mock<IGitHubClient>();
			var story = new Issue
			{
				Title = "[hook] Auto-linking to stories",
				Labels = new [] { new Label { Name = "Story" } },
			};
			var task = new Issue
			{
				Title = "[hook] Task about auto-linking",
				Labels = new [] { new Label { Name = "Task" } },
			};

			github.SetupGet(repository, task);
			github.SetupSearch(story);

			var linker = new AutoLink(github.Object);
			var update = new IssueUpdate();
			var updated = linker.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = new User { Login = "kzu" },
			}, update);

			Assert.True(updated);
			Assert.True(update.Body.Contains("#" + story.Number));
		}

		[Fact]
		public void when_processing_issue_then_automatically_links_with_closed_story()
		{
			var github = new Mock<IGitHubClient>();
			var story = new Issue
			{
				Title = "[hook] Auto-linking to closed stories",
				Labels = new [] { new Label { Name = "Story" } },
				State = ItemState.Closed,
			};
			var task = new Issue
			{
				Title = "[hook] Task about auto-linking to closed story",
				Labels = new [] { new Label { Name = "Task" } },
			};

			github.SetupGet(repository, task);
			github.SetupSearch(story);

			var linker = new AutoLink(github.Object);
			var update = new IssueUpdate();
			var updated = linker.Process(new Octokit.Events.IssuesEvent
				{
					Action = IssuesEvent.IssueAction.Opened,
					Issue = task,
					Repository = repository,
					Sender = repository.Owner,
				}, update);

			Assert.True(updated);
			Assert.True(update.Body.Contains("#" + story.Number), "Expected link to #" + story.Number + " but was '" + update.Body + "'.");
		}

		[Fact]
		public void when_processing_issue_with_existing_story_link_then_skips_processing()
		{
			var github = new Mock<IGitHubClient>(MockBehavior.Strict);
			var task = new Issue
			{
				Number = 1,
				Title = "[ui] Issue with story prefix",
				Body = "An issue with an existing story #2 link",
				Labels = new [] { new Label { Name = "Task" } }
			};

			github.SetupGet(repository, task);
			github.SetupGet(repository, new Issue
			{
				Number = 2,
				Labels = new [] { new Label { Name = "Story" } }
			});

			var linker = new AutoLink(github.Object);
			var update = new IssueUpdate();
			var updated = linker.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner,
			}, update);


			github.Verify(x => x.Issue.Get(repository.Owner.Login, repository.Name, 2));
			Assert.False(updated);
		}

		[Fact]
		public void when_processing_issue_other_links_then_updates_with_story_link_from_prefix()
		{
			var github = new Mock<IGitHubClient>(MockBehavior.Strict);
			var task = new Issue
			{
				Number = 1,
				Title = "[ui] Issue with story prefix",
				Body = "An issue with an existing issue #2 link",
				Labels = new [] { new Label { Name = "Task" } }
			};

			github.SetupGet(repository, task);
			github.SetupGet(repository, new Issue
			{
				Number = 2,
				Labels = new List<Label>()
			});
			github.SetupSearch(new Issue
			{
				Number = 3,
				Title = "[ui] Story"
			});

			var linker = new AutoLink(github.Object);
			var update = new IssueUpdate();
			var updated = linker.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner,
			}, update);

			Assert.True(updated);
			Assert.True(update.Body.Contains("#3"));
		}
	}
}
