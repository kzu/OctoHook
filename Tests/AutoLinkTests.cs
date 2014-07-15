namespace Tests
{
	using Moq;
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

			var story = (await github.Search.SearchIssues(new SearchIssuesRequest("[hook]")
			{
				Labels = new[] { "Story" },
				Repo = "kzu/sandbox",
				Type = IssueTypeQualifier.Issue,
				State = ItemState.Closed,
			})).Items.FirstOrDefault();

			if (story == null)
			{
				story = await github.Issue.Create(
					"kzu", "sandbox", new NewIssue("[hook] Auto-linking to closed stories")
					{
						Labels = { "Story" },
					});
			}

			if (story.State == ItemState.Open)
				await github.Issue.Update("kzu", "sandbox", story.Number, new IssueUpdate { State = ItemState.Closed });

			var task = await github.Issue.Create(
				"kzu", "sandbox", new NewIssue("[hook] Task about auto-linking to closed story")
				{
					Labels = { "Task" },
				});

			try
			{
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
			finally
			{
				github.Issue.Update("kzu", "sandbox", task.Number, new IssueUpdate { State = ItemState.Closed }).Wait();
				github.Issue.Update("kzu", "sandbox", story.Number, new IssueUpdate { State = ItemState.Closed }).Wait();
			}
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
				Labels = new List<Label>
				{
					new Label { Name = "Task" }
				}
			};

			github.Setup(x => x.Issue.Get("kzu", "repo", 1))
				.Returns(Task.FromResult(task));
			github.Setup(x => x.Issue.Get("kzu", "repo", 2))
				.Returns(Task.FromResult(new Issue
				{
					Number = 2,
					Labels = new List<Label>
					{
						new Label { Name = "Story" }
					}
				}));

			var linker = new AutoLink(github.Object);

			linker.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = new Repository
				{
					Owner = new User
					{
						Login = "kzu"
					},
					Name = "repo"
				},
				Sender = new User
				{
				},
			});

			github.Verify(x => x.Issue.Get("kzu", "repo", 2));
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
				Labels = new List<Label>
				{
					new Label { Name = "Task" }
				}
			};

			github.Setup(x => x.Issue.Get("kzu", "repo", 1))
				.Returns(Task.FromResult(task));
			github.Setup(x => x.Issue.Get("kzu", "repo", 2))
				.Returns(Task.FromResult(new Issue
				{
					Number = 2,
					Labels = new List<Label>()
				}));

			github.Setup(x => x.Search.SearchIssues(It.IsAny<SearchIssuesRequest>()))
				.Returns(Task.FromResult<SearchIssuesResult>(new SearchIssuesResult
				{
					Items = new List<Issue>
					{
						new Issue
						{
							Number = 3,
							Title = "[ui] Story"
						}
					}
				}));


			github.Setup(x => x.Issue.Update("kzu", "repo", 1, It.Is<IssueUpdate>(u => 
				u.Body.Contains("#3"))))
				.Returns(Task.FromResult(task));

			var linker = new AutoLink(github.Object);

			linker.Process(new Octokit.Events.IssuesEvent
			{
				Action = IssuesEvent.IssueAction.Opened,
				Issue = task,
				Repository = new Repository
				{
					Owner = new User
					{
						Login = "kzu"
					},
					Name = "repo"
				},
				Sender = new User
				{
				},
			});

			github.Verify(x => x.Issue.Update("kzu", "repo", 1, It.Is<IssueUpdate>(u => 
				u.Body.Contains("#3"))));
		}
	}
}
