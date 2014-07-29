namespace OctoHook.WebHooks
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
	/// Matches labels with the format "~[LABEL]", "-[LABEL]" or "+[LABEL]" at the end 
	/// of the sissue title and auto-applies them.
	/// </summary>
	/// <devdoc>
	/// See <see cref="IAutoUpdater"/> on why we need this abstraction.
	/// We keep applying updaters as long as their <see cref="IAutoUpdater.Apply"/>
	/// returns true, telling us that a change might have been applied to the issue 
	/// title.
	/// </devdoc>
	[Component]
	public class AutoUpdate : IWebHook<IssuesEvent>
	{
		static readonly ITracer tracer = Tracer.Get<AutoLink>();

		IGitHubClient github;
		IEnumerable<IAutoUpdater> updaters;

		public AutoUpdate(IGitHubClient github, IEnumerable<IAutoUpdater> updaters)
		{
			this.github = github;
			this.updaters = updaters;
		}

		public string Describe(IssuesEvent @event)
		{
			return string.Format("AutoUpdate https://github.com/{0}/{1}/issues/{2}",
				@event.Repository.Owner.Login,
				@event.Repository.Name,
				@event.Issue.Number);
		}

		public void Process(IssuesEvent @event)
		{
			ProcessAsync(@event).Wait();
		}

		private async Task ProcessAsync(IssuesEvent issue)
		{
			var update = new IssueUpdate { Title = issue.Issue.Title.Trim() };

			// Initialize updaters for the current issue request
			foreach (var updater in updaters)
			{
				updater.Initialize(issue);
			}

			var updated = false;

			while (updaters.Any(updater => updater.Apply(update)))
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