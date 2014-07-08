namespace Tests
{
    using Octokit;
    using Octokit.Internal;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Xunit;
	using System.Linq;
	using System.Threading.Tasks;

    public class OctoTests
    {
        static readonly Credentials credentials = new Credentials(File.ReadAllText(@"..\..\Token").Trim());

        [Fact]
        public void when_filtering_by_label_then_ANDS_the_result()
        {
            var github = new Octokit.Reactive.ObservableGitHubClient(
                new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));

            var issues = github.Issue.GetForRepository("xamarin", "XamarinVS", new RepositoryIssueRequest
            {
                Labels = { "Story", "story" }, 
                State = ItemState.Open,
                Since = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(30)), 
            });

            var stories = new List<Issue>();
            var done = false;
            var subscription = issues.Subscribe(
                i => stories.Add(i),
                () => done = true);

            while (!done)
            {
                Thread.Sleep(100);
            }

            // NOTE: we get NOTHING since the labels are AND'ed together.
            Assert.Equal(0, stories.Count);
        }

		[Fact]
		public async Task when_updating_issue_then_can_clear_assignee()
		{
            var github = new GitHubClient(
                new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));

            await github.Issue.Update("kzu", "sandbox", 56, new IssueUpdate
            {
                State = ItemState.Open,
                Assignee = "",
            });

			var issue = await github.Issue.Get("kzu", "sandbox", 56);

			Assert.Null(issue.Assignee);
		}

		[Fact]
		public async Task when_updating_issue_then_can_assign_non_existent_label()
		{
            var github = new GitHubClient(
                new ProductHeaderValue("kzu-client"), new InMemoryCredentialStore(credentials));

            await github.Issue.Update("kzu", "sandbox", 56, new IssueUpdate
            {
                State = ItemState.Open,
                Labels = { "foo" },
            });

			var issue = await github.Issue.Get("kzu", "sandbox", 56);

			Assert.True(issue.Labels.Any(l => l.Name == "foo"));
		}
	}
}
