namespace OctoHook
{
	using OctoHook.Diagnostics;
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Web;

	public class WorkQueue : IWorkQueue, IDisposable
	{
		static readonly ITracer tracer = Tracer.Get<WorkQueue>();

		BlockingCollection<Action> queue = new BlockingCollection<Action>();
		Thread worker;

		public WorkQueue()
		{
			tracer.Info("Started new worker queue.");
			worker = new Thread(ProcessWork);
			worker.Start();
		}

		public void Queue(Action work)
		{
			queue.Add(work);
		}

		private void ProcessWork()
		{
			foreach (var action in queue.GetConsumingEnumerable())
			{
				tracer.Info("Completing work.");
				action();
				tracer.Info("Completed work.");
			}
		}

		public void Dispose()
		{
			worker.Abort();
		}
	}
}