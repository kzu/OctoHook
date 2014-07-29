namespace OctoHook.WebHooks
{
	using Octokit;
	using Octokit.Events;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Base class for auto-updates driven by issue title hints
	/// </summary>
	/// <devdoc>
	/// We have this interface instead of a full IWebHook for each issue-driven 
	/// update since they all need to update the resulting issue title (remove 
	/// themselves from it, basically), and subsequent updaters may cause further 
	/// matches in otherwise non-matching ones (i.e. expect a hint at the end of 
	/// the issue title, would only match AFTER another updater removes its own 
	/// hint from the title).
	/// </devdoc>
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