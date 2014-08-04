namespace OctoHook
{
	using System;
	using System.Threading.Tasks;

	/// <summary>
	/// Webhook actions are processed asynchronously since they typically 
	/// need to do calls to other web services, and that might cause the 
	/// webhook to timeout.
	/// </summary>
	public interface IJobQueue
	{
		void Queue(Func<Task> work);
	}
}
