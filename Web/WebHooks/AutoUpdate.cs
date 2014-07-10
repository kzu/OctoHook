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
	[Component]
	public class AutoUpdate : IWebHook<IssuesEvent>
	{
		static readonly ITracer tracer = Tracer.Get<AutoLink>();

		IGitHubClient github;
		IEnumerable<AutoUpdater> updaters;

		public AutoUpdate(IGitHubClient github, IEnumerable<AutoUpdater> updaters)
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
			var issueTitle = issue.Issue.Title.Trim();
			if (!updaters.Any(r => r.IsMatch(issueTitle)))
			{
				tracer.Verbose("Skipping issue {0}/{1}#{2} without auto-updates.",
					issue.Repository.Owner.Login,
					issue.Repository.Name,
					issue.Issue.Number,
					issue.Issue.Title);
				return;
			}

			// Initialize updaters for the current issue request
			foreach (var updater in updaters)
			{
				updater.Initialize(issue);
			}

			var update = new IssueUpdate { Title = issueTitle };

			var match = updaters
				.Select(r => new { Updater = r, Match = r.Match(update.Title) })
				.FirstOrDefault(m => m.Match.Success);

			while (match != null)
			{
				match.Updater.Apply(match.Match, update);

				// Remove it from the title and try next match.
				update.Title = update.Title.Replace(match.Match.Value, "").Trim();

				match = updaters
					.Select(r => new { Updater = r, Match = r.Match(update.Title) })
					.FirstOrDefault(m => m.Match.Success);
			}

			await github.Issue.Update(issue.Repository.Owner.Login, issue.Repository.Name, issue.Issue.Number, update);

			var updates = new List<string>();
			if (update.Labels.Any())
				updates.Add(" labels [" + string.Join(", ", update.Labels) + "]");
			if (!string.IsNullOrEmpty(string.Join(", ", update.Labels)))
				updates.Add(" assignee '" + update.Assignee + "'");

			tracer.Info(@"Updated issue {0}/{1}#{2} with {3}.",
				issue.Repository.Owner.Login,
				issue.Repository.Name,
				issue.Issue.Number, 
				string.Join(", ", updates));
		}
	}
}