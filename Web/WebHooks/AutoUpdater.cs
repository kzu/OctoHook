namespace OctoHook.WebHooks
{
	using Octokit;
	using Octokit.Events;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Base class for auto-updates driven by issue title hints
	/// </summary>
	public abstract class AutoUpdater : Regex
	{
		protected AutoUpdater(string pattern, RegexOptions options)
			: base(pattern, options)
		{
		}

		/// <summary>
		/// Initializes the updater for the current issue being processed.
		/// </summary>
		public abstract void Initialize(IssuesEvent issue);

		/// <summary>
		/// Applies the update once the given match has already succeeded.
		/// </summary>
		public abstract void Apply(Match match, IssueUpdate update);
	}
}