namespace OctoHook.WebHooks
{
	using Octokit;
	using Octokit.Events;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Base class for auto-updates driven by issue title hints
	/// </summary>
	public interface IAutoUpdater
	{
		/// <summary>
		/// Initializes the updater for the current issue being processed.
		/// </summary>
		void Initialize(IssuesEvent issue);

		/// <summary>
		/// Applies the update to the issue.
		/// </summary>
		/// <returns><see langword="true"/> if the update process was applied; <see langword="false"/> otherwise.</returns>
		bool Apply(IssueUpdate update);
	}
}