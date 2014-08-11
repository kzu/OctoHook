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
            this.issuers = issuers.Select(i => new TracingOctoIssuer(tracer, i)).ToList();
        }

        public async Task ProcessAsync(IssuesEvent issue)
        {
            tracer.Verbose("Running IOctoIssuer components for https://github.com/{0}/{1}/issues/{2}",
                issue.Repository.Owner.Login,
                issue.Repository.Name,
                issue.Issue.Number);

            var update = new IssueUpdate { Title = issue.Issue.Title.Trim() };
            if (issue.Issue.Assignee != null)
                update.Assignee = issue.Issue.Assignee.Login;
            if (issue.Issue.Milestone != null)
                update.Milestone = issue.Issue.Milestone.Number;
            if (issue.Issue.Labels != null)
                update.Labels.AddRange(issue.Issue.Labels.Select(l => l.Name));

            var updated = false;

            while (issuers.Any(updater => updater.Process(issue, update)))
            {
                update.Title = update.Title.Trim();
                updated = true;
            }

            if (updated)
            {
                var labels = update.Labels.Distinct().ToArray();
                update.Labels.Clear();
                update.Labels.AddRange(labels);

                await github.Issue.Update(issue.Repository.Owner.Login, issue.Repository.Name, issue.Issue.Number, update);

                var updates = new List<string>();
                if (update.Labels.Any())
                    updates.Add(" labels [" + string.Join(", ", update.Labels) + "]");
                if (!string.IsNullOrEmpty(update.Assignee))
                    updates.Add(" assignee '" + update.Assignee + "'");
                if (!string.IsNullOrEmpty(update.Body))
                    updates.Add(" body '" + update.Body + "'");

                tracer.Info(@"Updated issue {0}/{1}#{2} with {3}.",
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

        private class TracingOctoIssuer : IOctoIssuer
        {
            ITracer tracer;
            IOctoIssuer issuer;
            string name;

            public TracingOctoIssuer(ITracer tracer, IOctoIssuer issuer)
            {
                this.tracer = tracer;
                this.issuer = issuer;
                this.name = issuer.GetType().Name;
            }

            public bool Process(IssuesEvent issue, IssueUpdate update)
            {
                tracer.Verbose("Processing with {0}", name);

                var result = issuer.Process(issue, update);

                if (result)
                    tracer.Info("{0} updated issue. All components will be run again to apply further updates to it.", name);
                else
                    tracer.Verbose("No changes performed by {0}", name);

                return result;
            }
        }
    }
}