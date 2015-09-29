﻿namespace OctoHook
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
	public class AutoLabel : IOctoIssuer
	{
		static readonly ITracer tracer = Tracer.Get<AutoLabel>();
		// \u2713 == ✓
		static readonly Regex expression = new Regex(@"\s(?<fullLabel>[\u2713|+|-](?<simpleLabel>([^\s]+|[""']\\?.*?)))$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		IGitHubClient github;
		List<string> labels;

		public AutoLabel(IGitHubClient github)
		{
			this.github = github;
		}

		public bool Process(IssuesEvent issue, IssueUpdate update)
		{
			var match = expression.Match(update.Title);
			if (!match.Success)
				return false;

			if (labels == null)
			{
				labels = github.Issue.Labels.GetForRepository(issue.Repository.Owner.Login, issue.Repository.Name)
					.Result
					.Select(l => l.Name)
					.ToList();
			}

		    var bareLabel = match.Groups["simpleLabel"].Value.Replace("\"", string.Empty);

			// Match label in case-insensitive manner, without the prefix first
			var label = labels.FirstOrDefault(l => string.Equals(l, bareLabel, StringComparison.OrdinalIgnoreCase));
			if (label == null)
				// Labels themselves could use the "+" sign, so we match next by the full string.
				label = labels.FirstOrDefault(l => string.Equals(l, match.Groups["fullLabel"].Value, StringComparison.OrdinalIgnoreCase));

			if (label != null)
			{
				update.AddLabel(label);
				tracer.Verbose("Applied pre-defined label '{0}'", label);
			}
			else
			{
				// Just apply the bare label as-is otherwise.
			    label = bareLabel;
				update.AddLabel(label);
				tracer.Verbose("Applied ad-hoc label '{0}'", label);
			}

			update.Title = update.Title.Replace(match.Value, "");

			return true;
		}
	}
}
