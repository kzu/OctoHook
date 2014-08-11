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
			var update = new IssueUpdate();

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
		public async Task when_octohook_begin_doesnt_exists_then_adds_it_automatically()
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
			var update = new IssueUpdate();

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner
			});

			github.Verify(x => x.Issue.Update(repository.Owner.Login, repository.Name, 2, It.Is<IssueUpdate>(u =>
				u.Body.Contains(AutoTask.SectionBegin))));
		}

		[Fact]
		public async Task when_octohook_end_doesnt_exists_then_adds_it_automatically()
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
			var update = new IssueUpdate();

			await linker.ProcessAsync(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = repository,
				Sender = repository.Owner
			});

			github.Verify(x => x.Issue.Update(repository.Owner.Login, repository.Name, 2, It.Is<IssueUpdate>(u =>
				u.Body.Contains(AutoTask.SectionEnd))));
		}

		[Fact]
		public async Task when_octohook_begin_exists_but_no_header_exists_then_it_is_not_added_again()
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
				Body = AutoTask.SectionBegin + Environment.NewLine + AutoTask.SectionEnd,
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

			github.Verify(x => x.Issue.Update(repository.Owner.Login, repository.Name, 2, It.Is<IssueUpdate>(u =>
				!u.Body.Contains(AutoTask.header))));
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
			var update = new IssueUpdate();

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
				Body = AutoTask.SectionBegin + 
					OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, task.Title) + 
					AutoTask.SectionEnd
			});

			var expectedLink = OctoHook.Properties.Strings.FormatTask("x", "#" + task.Number, task.Title);
			var linker = new AutoTask(github.Object);
			var update = new IssueUpdate();

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
				Body = AutoTask.SectionBegin + 
					OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, "Some old title") + 
					AutoTask.SectionEnd
			});

			// Expect same state but new title
			var expectedLink = OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, task.Title);
			var linker = new AutoTask(github.Object);
			var update = new IssueUpdate();

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
				Body = AutoTask.SectionBegin + 
					OctoHook.Properties.Strings.FormatTask("x", "#" + task.Number, task.Title) + 
					AutoTask.SectionEnd
			});

			var expectedLink = OctoHook.Properties.Strings.FormatTask(" ", "#" + task.Number, task.Title);
			var linker = new AutoTask(github.Object);
			var update = new IssueUpdate();

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
	}
}
