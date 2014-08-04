namespace OctoHook
{
	/// <summary>
	/// Interface implemented by GitHub webhooks that leverage 
	/// Octokit object model.
	/// </summary>
	/// <typeparam name="TEvent">Type of event processed by this webhook.</typeparam>
    public interface IOctoHook<TEvent>
    {
		/// <summary>
		/// Processes the specified event received by the webhook.
		/// </summary>
        void Process(TEvent @event);
    }
}