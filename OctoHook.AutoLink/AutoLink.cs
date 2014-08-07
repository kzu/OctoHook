namespace OctoHook
{
	using Octokit;
	using Octokit.Events;
	using System;
	using System.Text.RegularExpressions;
	using System.Linq;
	using System.Collections.ObjectModel;
	using OctoHook.CommonComposition;
	using System.Threading.Tasks;
	using OctoHook.Diagnostics;

	[Component]
	public class AutoLink : IOctoIssuer
	{
		static readonly ITracer tracer = Tracer.Get<AutoLink>();
		static readonly Regex storyPrefixExpr = new Regex(@"\[[^\]]+\]", RegexOptions.Compiled);
		static readonly Regex issueLink = new Regex(@"(?<=\#)\d+", RegexOptions.Compiled);

		private IGitHubClient github;

		public AutoLink(IGitHubClient github)
		{
			this.github = github;
		}

		public bool Process(IssuesEvent issue, IssueUpdate update)
		{
			tracer.Verbose("AutoLink::Process https://github.com/{0}/{1}/issues/{2}",
				issue.Repository.Owner.Login,
				issue.Repository.Name,
				issue.Issue.Number);

			return ProcessAsync(issue, update).Result;
		}

		private async Task<bool> ProcessAsync(IssuesEvent issue, IssueUpdate update)
		{
			var storyPrefix = storyPrefixExpr.Match(issue.Issue.Title);
			if (!storyPrefix.Success)
			{
				tracer.Verbose("Skipping issue {0}/{1}#{2} without a story prefix: '{3}'.",
					issue.Repository.Owner.Login,
					issue.Repository.Name,
					issue.Issue.Number,
					issue.Issue.Title);
				return false;
			}

			// Skip issues that are the story itself.
			if (issue.Issue.Labels.Any(l => string.Equals(l.Name, "story", StringComparison.OrdinalIgnoreCase)))
				return false;

			// Skip the issue if it already has a story link
			// Need to retrieve the full issue, since the event only contains the title
			var saved = await github.Issue.Get(issue.Repository.Owner.Login, issue.Repository.Name, issue.Issue.Number);
			if (!string.IsNullOrEmpty(saved.Body))
			{
				foreach (var number in issueLink.Matches(saved.Body).OfType<Match>().Where(m => m.Success).Select(m => int.Parse(m.Value)))
				{
					try
					{
						var linkedIssue = await github.Issue.Get(issue.Repository.Owner.Login, issue.Repository.Name, number);
						if (linkedIssue.Labels.Any(l => string.Equals(l.Name, "story", StringComparison.OrdinalIgnoreCase)))
						{
							tracer.Info("Skipping issue {0}/{1}#{2} as it already contains story link to #{3}.",
								issue.Repository.Owner.Login,
								issue.Repository.Name,
								issue.Issue.Number,
								number);
							return false;
						}
					}
					catch (NotFoundException)
					{
						// It may be a link to a bug/issue in another system.
					}
				}
			}

			// Find the story with the same prefix.
			var repository = issue.Repository.Owner.Login + "/" + issue.Repository.Name;
			var story = await FindStoryAsync(repository, storyPrefix.Value);
			if (story == null)
			{
				tracer.Warn("Issue {0}/{1}#{2} has story prefix '{3}' but no matching issue with the label 'Story' or 'story' was found with such prefix.",
					issue.Repository.Owner.Login,
					issue.Repository.Name,
					issue.Issue.Number,
					storyPrefix.Value);
				return false;
			}

			update.State = saved.State;
			update.Body = (saved.Body == null ? "" : saved.Body + @"

")
					+ "Story #" + story.Number;

			tracer.Info("Established new story link between issue {0}/{1}#{2} and story #{3}.",
				issue.Repository.Owner.Login,
				issue.Repository.Name,
				issue.Issue.Number,
				story.Number);

			return true;
		}

		private async Task<Issue> FindStoryAsync(string repository, string query)
		{
			var story = await FindIssueAsync(repository, query, ItemState.Open, "Story");
			if (story == null)
				story = await FindIssueAsync(repository, query, ItemState.Open, "story");
			if (story == null)
				story = await FindIssueAsync(repository, query, ItemState.Closed, "Story");
			if (story == null)
				story = await FindIssueAsync(repository, query, ItemState.Closed, "story");

			return story;
		}

		private async Task<Issue> FindIssueAsync(string repository, string query, ItemState state, string label)
		{
			tracer.Verbose("Querying for '{0}' on repo '{1}' with state '{2}' and label '{3}'.",
				query, repository, state, label);

			var stories = await github.Search.SearchIssues(new SearchIssuesRequest(query)
			{
				Labels = new[] { label },
				Repo = repository,
				Type = IssueTypeQualifier.Issue,
				State = state,
				// Always point to newest found first.
				Order = SortDirection.Descending,
			});

			tracer.Verbose("Results: {0}.", stories.TotalCount);

			return stories.Items.FirstOrDefault();
		}
	}
}