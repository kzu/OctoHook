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

	public class AutoTaskTests
	{
		static readonly Credentials credentials = TestCredentials.Create();

		static readonly Repository repository = new Repository
			{
				Owner = new User
				{
					Login = "kzu"
				},
				Name = "autotask",
			};

		[Fact]
		public async Task when_octohook_header_doesnt_exists_then_adds_it_automatically()
		{
			var github = new Mock<IGitHubClient>();
			var task = new Issue
			{
				Number = 1,
				Title = "Issue with story link",
				Body = "Story #2",
			};

			github.SetupGet(repository, task);
			github.SetupGet(repository, new Issue
			{
				Number = 2,
				Title = "Story"
			});

			var linker = new AutoTask(github.Object);

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner
			});

			github.Verify(x => x.Issue.Update(repository.Owner.Login, repository.Name, 2, It.Is<IssueUpdate>(u =>
				u.Body.Contains(AutoTask.header))));
		}

		[Fact]
		public async Task when_task_list_link_doesnt_exists_then_adds_it_automatically()
		{
			var github = new Mock<IGitHubClient>();
			var task = new Issue
			{
				Number = 1,
				Title = "Issue with story link",
				Body = "Story #2",
			};

			github.SetupGet(repository, task);
			github.SetupGet(repository, new Issue
			{
				Number = 2,
				Title = "Story"
			});

			var expectedLink = OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, task.Title);
			var linker = new AutoTask(github.Object);

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner
			});

			github.Verify(x => x.Issue.Update(repository.Owner.Login, repository.Name, 2, It.Is<IssueUpdate>(u =>
				u.Body.Contains(expectedLink))));
		}

		[Fact]
		public async Task when_task_list_link_exists_then_updates_its_state()
		{
			var github = new Mock<IGitHubClient>();
			var task = new Issue
			{
				Number = 1,
				Title = "Issue with story link",
				Body = "Story #2",
				State = ItemState.Closed,
			};

			github.SetupGet(repository, task);
			github.SetupGet(repository, new Issue
			{
				Number = 2,
				Title = "Story",
				Body = AutoTask.header +
					OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, task.Title)
			});

			var expectedLink = OctoHook.Properties.Strings.FormatTask("x", "#" + task.Number, task.Title);
			var linker = new AutoTask(github.Object);

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner
			});

			github.Verify(x => x.Issue.Update(repository.Owner.Login, repository.Name, 2, It.Is<IssueUpdate>(u =>
				u.Body.Contains(expectedLink))));
		}

		[Fact]
		public async Task when_task_list_link_exists_then_updates_its_title()
		{
			var github = new Mock<IGitHubClient>();
			var task = new Issue
			{
				Number = 1,
				Title = "Issue with story link",
				Body = "Story #2",
			};

			github.SetupGet(repository, task);
			github.SetupGet(repository, new Issue
			{
				Number = 2,
				Title = "Story",
				Body = AutoTask.header +
					OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, "Some old title")
			});

			// Expect same state but new title
			var expectedLink = OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, task.Title);
			var linker = new AutoTask(github.Object);

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner
			});

			github.Verify(x => x.Issue.Update(repository.Owner.Login, repository.Name, 2, It.Is<IssueUpdate>(u =>
				u.Body.Contains(expectedLink))));
		}

		[Fact]
		public async Task when_task_list_exists_then_appends_to_list()
		{
			var github = new Mock<IGitHubClient>();
			var task = new Issue
			{
				Number = 1,
				Title = "Issue with story link",
				Body = "Story #2",
			};

			github.SetupGet(repository, task);
			github.SetupGet(repository, new Issue
			{
				Number = 2,
				Title = "Story",
				Body =
					AutoTask.header +
					OctoHook.Properties.Strings.FormatTask(" ", "#5", "Existing task5") + Environment.NewLine +
					OctoHook.Properties.Strings.FormatTask(" ", "#6", "Existing task6")
			});

			var body = "";
			github.Setup(x => x.Issue.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IssueUpdate>()))
				.Callback<string, string, int, IssueUpdate>((_, __, ___, update) => body = update.Body)
				.ReturnsAsync(null);

			var expectedLink = OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, task.Title);
			var linker = new AutoTask(github.Object);

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner
			});

			github.Verify(x => x.Issue.Update(repository.Owner.Login, repository.Name, 2, It.Is<IssueUpdate>(u =>
				Regex.Matches(u.Body, @"- \[ \]").Count == 3)));
		}

		[Fact]
		public async Task when_task_list_link_reopened_then_updates_its_state()
		{
			var github = new Mock<IGitHubClient>();
			var task = new Issue
			{
				Number = 1,
				Title = "Issue with story link",
				Body = "Story #2",
				State = ItemState.Open,
			};

			github.SetupGet(repository, task);
			github.SetupGet(repository, new Issue
			{
				Number = 2,
				Title = "Story",
				Body = OctoHook.Properties.Strings.FormatTask("x", "#" + task.Number, task.Title)
			});

			var expectedLink = OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, task.Title);
			var linker = new AutoTask(github.Object);

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner
			});

			github.Verify(x => x.Issue.Update(repository.Owner.Login, repository.Name, 2, It.Is<IssueUpdate>(u =>
				u.Body.Contains(expectedLink))));
		}

		[Fact]
		public async Task when_task_list_link_exists_and_matches_then_does_not_update()
		{
			var github = new Mock<IGitHubClient>();
			var task = new Issue
			{
				Number = 1,
				Title = "Issue with story link",
				Body = "Story #2",
			};

			github.SetupGet(repository, task);
			github.SetupGet(repository, new Issue
			{
				Number = 2,
				Title = "Story",
				Body = OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, task.Title)
			});

			var linker = new AutoTask(github.Object);
			var update = new IssueUpdate();

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner
			});

			github.Verify(x => x.Issue.Update(repository.Owner.Login, repository.Name, 2, It.IsAny<IssueUpdate>()),
				Times.Never());
		}

		[Fact]
		public async Task when_link_comes_from_task_list_then_does_not_process_it()
		{
			var github = new Mock<IGitHubClient>(MockBehavior.Strict);
			var task = new Issue
			{
				Number = 1,
				Title = "Issue with story link",
				Body = "- [ ] #2 A Story",
			};

			github.SetupGet(repository, task);

			var linker = new AutoTask(github.Object);
			var update = new IssueUpdate();

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner
			});
		}

		[Fact]
		public async Task when_task_list_link_doesnt_exists_then_adds_it_automatically_integration()
		{
			var github = new GitHubClient(new ProductHeaderValue("octohook"), new InMemoryCredentialStore(credentials));
			var story = await github.Issue.Create("kzu", "sandbox", new NewIssue("Story"));
			var task = await github.Issue.Create("kzu", "sandbox", new NewIssue("Issue with story link")
			{
				Body = "Story #" + story.Number,
			});

			var expectedLink = OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, task.Title);
			var linker = new AutoTask(github);
			var update = new IssueUpdate();

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = new Repository
				{
					Name = "sandbox",
					Owner = new User { Login = "kzu" },
				},
				Sender = new User { Login = "kzu" }
			});

			var updated = await github.Issue.Get("kzu", "sandbox", story.Number);

			Assert.True(updated.Body.Contains(expectedLink));

			await github.Issue.Update("kzu", "sandbox", task.Number, new IssueUpdate { State = ItemState.Closed });
			await github.Issue.Update("kzu", "sandbox", story.Number, new IssueUpdate { State = ItemState.Closed });
		}

		[Fact]
		public async Task when_linked_task_is_closed_then_doesnt_reopen_it_integration()
		{
			var github = new GitHubClient(new ProductHeaderValue("octohook"), new InMemoryCredentialStore(credentials));
			var parent = await github.Issue.Create("kzu", "sandbox", new NewIssue("Parent"));
			var child = await github.Issue.Create("kzu", "sandbox", new NewIssue("Issue with link to parent")
			{
				Body = "Related to #" + parent.Number,
			});
			// Close the parent.
			await github.Issue.Update("kzu", "sandbox", parent.Number, new IssueUpdate { State = ItemState.Closed });

			try
			{
				Assert.Equal(ItemState.Closed, github.Issue.Get("kzu", "sandbox", parent.Number).Result.State);

				var expectedLink = OctoHook.Properties.Strings.FormatTask(" ", "#" + child.Number, child.Title);
				var tasker = new AutoTask(github);

				await tasker.ProcessAsync(new Octokit.Events.IssuesEvent
				{
					Action = IssuesEvent.IssueAction.Opened,
					Issue = child,
					Repository = new Repository
					{
						Name = "sandbox",
						Owner = new User { Login = "kzu" },
					},
					Sender = new User { Login = "kzu" }
				});

				var updated = await github.Issue.Get("kzu", "sandbox", parent.Number);

				Assert.Equal(ItemState.Closed, updated.State);
			}
			finally
			{
				github.Issue.Update("kzu", "sandbox", child.Number, new IssueUpdate { State = ItemState.Closed }).Wait();
				github.Issue.Update("kzu", "sandbox", parent.Number, new IssueUpdate { State = ItemState.Closed }).Wait();
			}
		}
	}
}
