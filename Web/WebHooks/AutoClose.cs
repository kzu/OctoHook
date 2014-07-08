namespace OctoHook.WebHooks
{
	using Octokit;
	using Octokit.Events;
	using System.Text.RegularExpressions;
	using System.Linq;
	using System.Threading.Tasks;
	using OctoHook.CommonComposition;
	using OctoHook.Diagnostics;

	/// <summary>
	/// Forcedly closes issues even if they aren't closed automatically by GitHub, 
	/// which happens when the associated commit is pushed to a non-default branch.
	/// </summary>
	[Component]
	public class AutoClose : IWebHook<PushEvent>
	{
		static readonly ITracer tracer = Tracer.Get<AutoClose>();
		static readonly Regex CloseExpr = new Regex(@"(close[s|d]?|fix(es|ed)?|resolve[s|d]?)",
				RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		static Regex IssueNumberExpr = new Regex(@"(?<=\#)\d+", RegexOptions.Compiled);

		private IGitHubClient github;

		public AutoClose(IGitHubClient github)
		{
			this.github = github;
		}

		public void Process(PushEvent @event)
		{
			ProcessAsync(@event).Wait();
		}

		public async Task ProcessAsync(PushEvent @event)
		{
			var closingCommits = @event.Commits.Where(c => CloseExpr.IsMatch(c.Message)).ToArray();
			if (closingCommits.Length == 0)
			{
				tracer.Verbose("There are no commits to process that have a close/fix/resolve message.");
				return;
			}

			tracer.Verbose("Found {0} commits to process that have a close/fix/resolve message.", closingCommits.Length);

			var closedIssues = closingCommits
				.SelectMany(c => IssueNumberExpr
					.Matches(c.Message)
					.OfType<Match>()
					.Select(m => int.Parse(m.Value)))
				.Distinct()
				.Select(number => github.Issue.Get(
					@event.Repository.Owner.Name ?? @event.Repository.Owner.Login,
					@event.Repository.Name,
					number));

			foreach (var closedIssue in closedIssues)
			{
				var issue = await closedIssue;
				if (issue.State == ItemState.Closed)
				{
					tracer.Verbose("Skipping issue #{0} as it was already closed.", issue.Number);
					continue;
				}

				await github.Issue.Update(
					@event.Repository.Owner.Name ?? @event.Repository.Owner.Login,
					@event.Repository.Name,
					issue.Number,
					new IssueUpdate
					{
						State = ItemState.Closed,
						Assignee = "",
					});

				if (issue.Assignee != null)
					tracer.Info("Closed issue #{0} automatically and unassigned from '{1}'.", issue.Number, issue.Assignee.Login);
				else
					tracer.Info("Closed issue #{0} automatically.", issue.Number);
			}
		}
	}
}