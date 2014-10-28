namespace Tests
{
    using Newtonsoft.Json.Linq;
    using OctoHook;
    using OctoHook.Web;
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

    public class AutoUpdateTests
    {
        static readonly Credentials credentials = new Credentials(File.ReadAllText(@"..\..\Token").Trim());

        [Fact]
        public async Task when_processing_issue_with_lower_case_label_then_automatically_adds_labels()
        {
            var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));
            var repository = await github.Repository.Get("kzu", "sandbox");
            var user = await github.User.Current();

            var issue = await github.Issue.Create(
                "kzu", "sandbox", new NewIssue("Auto-labeling to stories +story"));

            var labeler = new OctoIssuerJob(github, new IOctoIssuer[] { new AutoLabel(github) });

            await labeler.ProcessAsync(new Octokit.Events.IssuesEvent
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

            var labeler = new OctoIssuerJob(github, new IOctoIssuer[] { new AutoLabel(github) });

            await labeler.ProcessAsync(new Octokit.Events.IssuesEvent
            {
                Action = IssuesEvent.IssueAction.Opened,
                Issue = issue,
                Repository = repository,
                Sender = user,
            });

            var updated = await github.Issue.Get("kzu", "sandbox", issue.Number);

            Assert.Equal("Auto-labeling to ~foo in the middle doesn't work", updated.Title);
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

            var labeler = new OctoIssuerJob(github, new IOctoIssuer[] { new AutoLabel(github) });

            await labeler.ProcessAsync(new Octokit.Events.IssuesEvent
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
        public async Task when_processing_issue_with_declared_minus_label_then_applies_it_with_minus()
        {
            var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));
            var repository = await github.Repository.Get("kzu", "sandbox");
            var user = await github.User.Current();

            var issue = await github.Issue.Create(
                "kzu", "sandbox", new NewIssue("Auto-labeling to -qa"));

            var labeler = new OctoIssuerJob(github, new IOctoIssuer[] { new AutoLabel(github) });

            await labeler.ProcessAsync(new Octokit.Events.IssuesEvent
            {
                Action = IssuesEvent.IssueAction.Opened,
                Issue = issue,
                Repository = repository,
                Sender = user,
            });

            var updated = await github.Issue.Get("kzu", "sandbox", issue.Number);

            Assert.Equal("Auto-labeling to", updated.Title);
            Assert.True(updated.Labels.Any(l => l.Name == "-QA"));

            await github.Issue.Update("kzu", "sandbox", issue.Number, new IssueUpdate { State = ItemState.Closed });
        }

        [Fact]
        public async Task when_processing_issue_with_declared_check_label_then_applies_it_with_check()
        {
            var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));
            var repository = await github.Repository.Get("kzu", "sandbox");

			var label = await github.Issue.Labels.Get("kzu", "sandbox", "✓QA");
			if (label == null)
				await github.Issue.Labels.Create("kzu", "sandbox", new NewLabel("✓QA", "#bfd4f2"));

            var user = await github.User.Current();

            var issue = await github.Issue.Create(
                "kzu", "sandbox", new NewIssue("Auto-labeling to ✓qa"));

            var labeler = new OctoIssuerJob(github, new IOctoIssuer[] { new AutoLabel(github) });

            await labeler.ProcessAsync(new Octokit.Events.IssuesEvent
            {
                Action = IssuesEvent.IssueAction.Opened,
                Issue = issue,
                Repository = repository,
                Sender = user,
            });

            var updated = await github.Issue.Get("kzu", "sandbox", issue.Number);

            Assert.Equal("Auto-labeling to", updated.Title);
            Assert.True(updated.Labels.Any(l => l.Name == "✓QA"));

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

            var labeler = new OctoIssuerJob(github, new IOctoIssuer[] { new AutoLabel(github) });

            try
            {
                await github.Issue.Labels.Delete("kzu", "sandbox", "foo");
            }
            catch { }

            await labeler.ProcessAsync(new Octokit.Events.IssuesEvent
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

        [Fact]
        public async Task when_processing_issue_with_colon_me_then_assigns_to_me()
        {
            var github = new GitHubClient(new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));

            var repository = await github.Repository.Get("kzu", "sandbox");
            var user = await github.User.Current();

            var issue = await github.Issue.Create(
                "kzu", "sandbox", new NewIssue("Auto-assigning to :me"));

            var updater = new OctoIssuerJob(github, new IOctoIssuer[] { new AutoAssign() });

            await updater.ProcessAsync(new Octokit.Events.IssuesEvent
            {
                Action = IssuesEvent.IssueAction.Opened,
                Issue = issue,
                Repository = repository,
                Sender = user,
            });

            var updated = await github.Issue.Get("kzu", "sandbox", issue.Number);

            Assert.Equal("Auto-assigning to", updated.Title);
            Assert.Equal("kzu", updated.Assignee.Login);

            await github.Issue.Update("kzu", "sandbox", issue.Number, new IssueUpdate { State = ItemState.Closed });
        }
    }
}
