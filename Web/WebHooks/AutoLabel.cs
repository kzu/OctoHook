namespace OctoHook.WebHooks
{
	using OctoHook.CommonComposition;
	using OctoHook.Diagnostics;
	using Octokit;
	using Octokit.Events;
	using System;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;

	/// <summary>
	/// Matches labels with the format "~[LABEL]" or "+[LABEL]" at the end 
	/// of the sissue title and auto-applies them.
	/// </summary>
	[Component]
	public class AutoLabel : IWebHook<IssuesEvent>
	{
		static readonly ITracer tracer = Tracer.Get<AutoLink>();
		static readonly Regex labelExpr = new Regex(@"(?<full>[~|+](?<bare>[^\s]+))$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		private IGitHubClient github;

		public AutoLabel(IGitHubClient github)
		{
			this.github = github;
		}

		public void Process(IssuesEvent @event)
		{
			ProcessAsync(@event).Wait();
		}

		private async Task ProcessAsync(IssuesEvent @event)
		{
			var issueTitle = @event.Issue.Title.Trim();
			var labelMatch = labelExpr.Match(issueTitle);
			if (!labelMatch.Success)
			{
				tracer.Verbose("Skipping issue #{0} without auto-labels.", @event.Issue.Number, @event.Issue.Title);
				return;
			}

			var definedLabels = (await github.Issue.Labels.GetForRepository(@event.Repository.Owner.Login, @event.Repository.Name))
				.Select(l => l.Name)
				.ToList();

			var updateIssue = new IssueUpdate { Title = issueTitle };
			while (labelMatch.Success)
			{
				// Match label in case-insensitive manner
				var label = definedLabels.FirstOrDefault(l => string.Equals(l, labelMatch.Groups["bare"].Value, StringComparison.OrdinalIgnoreCase));
				if (label == null)
					// Labels themselves could use the "+" or "~" sign, so we match next by the full string.
					label = definedLabels.FirstOrDefault(l => string.Equals(l, labelMatch.Groups["full"].Value, StringComparison.OrdinalIgnoreCase));

				if (label != null)
					updateIssue.Labels.Add(label);
				else
					// Just apply the bare label as-is otherwise.
					updateIssue.Labels.Add(labelMatch.Groups["bare"].Value);

				// Remove it from the title and try next match.
				updateIssue.Title = labelExpr.Replace(updateIssue.Title, "").Trim();
				labelMatch = labelExpr.Match(updateIssue.Title);
			}

			await github.Issue.Update(@event.Repository.Owner.Login, @event.Repository.Name, @event.Issue.Number, updateIssue);
		}
	}
}