namespace OctoHook
{
	using System.Threading.Tasks;

	/// <summary>
	/// Interface implemented by async jobs triggered from 
	/// GitHub webhooks, which leverage Octokit object model 
	/// and take longer to execute.
	/// </summary>
	/// <remarks>
	/// This interface allows hooks that would otherwise cause 
	/// timeout errors on the GitHub side to run successfully to 
	/// completion, since the response to GitHub is an immediate 
	/// success. Further logging at the destination service is 
	/// required to actually determine execution outcome, so 
	/// these hooks are a bit harder to diagnose.
	/// </remarks>
	/// <typeparam name="TEvent">Type of event processed by this webhook.</typeparam>
	public interface IOctoJob<TEvent>
	{
		/// <summary>
		/// Processes the specified event received by the webhook in 
		/// an asynchronous fashion, from a background job scheduler 
		/// after the webhook execution returns success to GitHub.
		/// </summary>
		Task ProcessAsync(TEvent @event);
	}
}