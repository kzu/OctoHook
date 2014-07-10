namespace OctoHook.WebHooks
{
	using OctoHook.CommonComposition;
using OctoHook.Diagnostics;
using Octokit;
using Octokit.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

	[Component]
	public class AutoLabel : AutoUpdater
	{
		static readonly ITracer tracer = Tracer.Get<AutoLabel>();

		IGitHubClient github;
		List<string> labels;

		public AutoLabel(IGitHubClient github)
			: base(@"(?<fullLabel>[~|+|-](?<simpleLabel>[^\s]+))$", RegexOptions.Compiled | RegexOptions.ExplicitCapture)
		{
			this.github = github;
		}

		public override void Apply(Match match, IssueUpdate update)
		{
			// Match label in case-insensitive manner
			var label = labels.FirstOrDefault(l => string.Equals(l, match.Groups["fullLabel"].Value, StringComparison.OrdinalIgnoreCase));
			if (label == null)
				// Labels themselves could use the "+" or "~" sign, so we match next by the full string.
				label = labels.FirstOrDefault(l => string.Equals(l, match.Groups["fullLabel"].Value, StringComparison.OrdinalIgnoreCase));

			if (label != null)
			{
				update.Labels.Add(label);
				tracer.Verbose("Applied pre-defined label '{0}'", label);
			}
			else
			{ 
				// Just apply the bare label as-is otherwise.
				label = match.Groups["simpleLabel"].Value;
				update.Labels.Add(label);
				tracer.Verbose("Applied ad-hoc label '{0}'", label);
			}
		}

		public override void Initialize(IssuesEvent issue)
		{
			labels = github.Issue.Labels.GetForRepository(issue.Repository.Owner.Login, issue.Repository.Name)
				.Result
				.Select(l => l.Name)
				.ToList();
		}
	}
}