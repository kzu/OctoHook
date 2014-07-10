namespace OctoHook.WebHooks
{
	using OctoHook.CommonComposition;
	using Octokit;
	using Octokit.Events;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Web;

	[Component]
	public class AutoAssign : AutoUpdater
	{
		private IssuesEvent issue;

		public AutoAssign()
			: base(@":(?<user>\w+)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture)
		{
		}

		public override void Apply(Match match, IssueUpdate update)
		{
			var login = match.Groups["user"].Value;
			if (string.Equals(login, "me", StringComparison.OrdinalIgnoreCase))
				update.Assignee = issue.Sender.Login;
			else
				update.Assignee = login;
		}

		public override void Initialize(IssuesEvent issue)
		{
			this.issue = issue;
		}
	}
}