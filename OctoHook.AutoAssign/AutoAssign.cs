namespace OctoHook
{
	using OctoHook.CommonComposition;
	using Octokit;
	using Octokit.Events;
	using System;
	using System.Text.RegularExpressions;

	[Component]
	public class AutoAssign : IOctoIssuer
	{
		static readonly Regex expression = new Regex(@":(?<user>\w+)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		public bool Process(IssuesEvent issue, IssueUpdate update)
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
	}
}