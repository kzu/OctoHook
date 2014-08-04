namespace OctoHook
{
	using Newtonsoft.Json;
	using OctoHook.CommonComposition;
	using OctoHook.Diagnostics;
	using Octokit;
	using Octokit.Events;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;

	/// <summary>
	/// Processes available <see cref="IOctoIssuer"/> components for 
	/// the received issue webhook event.
	/// </summary>
	/// <devdoc>
	/// See <see cref="IOctoIssuer"/> on why we need this abstraction.
	/// We keep applying updaters as long as their <see cref="IOctoIssuer.Process"/>
	/// returns true, telling us that a change might have been applied to the issue 
	/// title.
	/// </devdoc>
	[Component]
	public class OctoIssuerJob : IOctoJob<IssuesEvent>
	{
		static readonly ITracer tracer = Tracer.Get<OctoIssuerJob>();

		IGitHubClient github;
		IEnumerable<IOctoIssuer> issuers;

		public OctoIssuerJob(IGitHubClient github, IEnumerable<IOctoIssuer> issuers)
		{
			this.github = github;
			this.issuers = issuers;
		}

		public async Task ProcessAsync(IssuesEvent issue)
		{
			tracer.Verbose("AutoUpdate::ProcessAsync https://github.com/{0}/{1}/issues/{2}",
				issue.Repository.Owner.Login,
				issue.Repository.Name,
				issue.Issue.Number);

			var update = new IssueUpdate { Title = issue.Issue.Title.Trim() };
			var updated = false;

			while (issuers.Any(updater => updater.Process(issue, update)))
			{
				update.Title = update.Title.Trim();
				updated = true;
			}

			if (updated)
			{
				await github.Issue.Update(issue.Repository.Owner.Login, issue.Repository.Name, issue.Issue.Number, update);

				var updates = new List<string>();
				if (update.Labels.Any())
					updates.Add(" labels [" + string.Join(", ", update.Labels) + "]");
				if (!string.IsNullOrEmpty(update.Assignee))
					updates.Add(" assignee '" + update.Assignee + "'");

				tracer.Info(@"Updated issue {0}/{1}#{2} with{3}.",
					issue.Repository.Owner.Login,
					issue.Repository.Name,
					issue.Issue.Number,
					string.Join(", ", updates));
			}
			else
			{
				tracer.Verbose(@"Skipped issue {0}/{1}#{2} since it had no applicable auto-updates.",
					issue.Repository.Owner.Login,
					issue.Repository.Name,
					issue.Issue.Number);
			}
		}
	}
}