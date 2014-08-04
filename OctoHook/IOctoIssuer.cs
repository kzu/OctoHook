namespace OctoHook
{
	using Octokit;
	using Octokit.Events;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Interface implemented by components that provide auto-updates 
	/// from issue events.
	/// </summary>
	/// <devdoc>
	/// We have this interface instead of a full IOctoHook for each issue-driven 
	/// update since they may need to update the resulting issue title or body 
	/// (i.e. remove themselves from it, basically, like the auto-labels), and 
	/// subsequent updaters may cause further matches in otherwise non-matching 
	/// ones (i.e. expect a hint at the end of the issue title, would only match 
	/// AFTER another updater removes its own hint from the title).
	/// </devdoc>
	public interface IOctoIssuer
	{
		/// <summary>
		/// Processes the specified issue event received by the webhook, and 
		/// optionally provides an update to it.
		/// </summary>
		/// <remarks>
		/// All issue updaters are invoked until none return <see langword="true"/>, which 
		/// indicates that no more changes are needed on the issue.
		/// </remarks>
		/// <returns><see langword="true"/> if a change was applied to the <paramref name="update"/>
		/// argument; <see langword="false"/> otherwise.</returns>
		bool Process(IssuesEvent issue, IssueUpdate update);
	}
}
