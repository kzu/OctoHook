namespace OctoHook.WebHooks
{
	using OctoHook.CommonComposition;
	using Octokit;
	using Octokit.Events;
	using System;
	using System.Text.RegularExpressions;

	[Component]
	public class AutoAssign : IAutoUpdater
	{
		static readonly Regex expression = new Regex(@":(?<user>\w+)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		IssuesEvent issue;

		public bool Apply(IssueUpdate update)
		{
			var match = expression.Match(update.Title);
			if (!match.Success)
				return false;

			var login = match.Groups["user"].Value;
			if (string.Equals(login, "me", StringComparison.OrdinalIgnoreCase))
				update.Assignee = issue.Sender.Login;
			else
				update.Assignee = login;

			update.Title = update.Title.Replace(match.Value, "");

			return true;
		}

		public void Initialize(IssuesEvent issue)
		{
			this.issue = issue;
		}
	}
}