namespace OctoHook
{
	using Octokit;
	using Octokit.Events;
	using System.Text.RegularExpressions;
	using System.Linq;
	using System.Threading.Tasks;
	using OctoHook.CommonComposition;
	using OctoHook.Diagnostics;
	using System.Collections.Generic;
	using System;

	/// <summary>
	/// Forcedly closes issues even if they aren't closed automatically by GitHub, 
	/// which happens when the associated commit is pushed to a non-default branch.
	/// </summary>
	[Component]
	public class AutoClose : IOctoJob<PushEvent>
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

		public async Task ProcessAsync(PushEvent @event)
		{
			tracer.Verbose("ProcessAsync https://github.com/{0}/{1}/commit/{2}",
				@event.Repository.Owner.Name ?? @event.Repository.Owner.Login,
				@event.Repository.Name,
				@event.HeadCommit.Sha.Substring(0, 6));

			var closingCommits = @event.Commits.Where(c => CloseExpr.IsMatch(c.Message) && IssueNumberExpr.IsMatch(c.Message))
				.Distinct(new SelectorComparer<PushEvent.CommitInfo, string>(c => c.Sha))
				.ToArray();
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
					.Select(m => new
					{
						Commit = c.Sha,
						IssueNumber = int.Parse(m.Value),
						GetIssue = github.Issue.Get(
							@event.Repository.Owner.Name ?? @event.Repository.Owner.Login,
							@event.Repository.Name,
							int.Parse(m.Value)),
					})
				);

			foreach (var closedIssue in closedIssues)
			{
				Issue issue;
				try
				{
					issue = await closedIssue.GetIssue;
				}
				catch (NotFoundException)
				{
					tracer.Warn("Referred issue #{0} does not exist in repository {1}/{2}.",
						closedIssue.IssueNumber,
						@event.Repository.Owner.Name ?? @event.Repository.Owner.Login,
						@event.Repository.Name);

					continue;
				}

				if (issue.State == ItemState.Closed && issue.Assignee == null)
				{
					tracer.Verbose("Skipping issue {0}/{1}#{2} as it was already closed and unassigned.",
						@event.Repository.Owner.Name ?? @event.Repository.Owner.Login,
						@event.Repository.Name,
						issue.Number);

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
					tracer.Info("Closed issue {0}/{1}#{2} automatically and unassigned from '{3}'.",
						@event.Repository.Owner.Name ?? @event.Repository.Owner.Login,
						@event.Repository.Name,
						issue.Number,
						issue.Assignee.Login);
				else
					tracer.Info("Closed issue {0}/{1}#{2} automatically.",
						@event.Repository.Owner.Name ?? @event.Repository.Owner.Login,
						@event.Repository.Name,
						issue.Number);
			}
		}

		private class SelectorComparer<T, TResult> : IEqualityComparer<T>
		{
			private Func<T, TResult> selector;

			public SelectorComparer(Func<T, TResult> selector)
			{
				this.selector = selector;
			}

			public bool Equals(T x, T y)
			{
				return Object.Equals(selector(x), selector(y));
			}

			public int GetHashCode(T obj)
			{
				return selector(obj).GetHashCode();
			}
		}
	}
}