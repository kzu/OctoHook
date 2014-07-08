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

		BlockingCollection<Task> tasks = new BlockingCollection<Task>();
		Thread worker;

		public WorkQueue()
		{
			tracer.Info("Started new worker queue.");
			worker = new Thread(ProcessWork);
			worker.Start();
		}

		public void Queue(Task work)
		{
			tracer.Info("New work queued");
			tasks.Add(work);
		}

		private void ProcessWork()
		{
			foreach (var task in tasks.GetConsumingEnumerable())
			{
				using (tracer.StartActivity("Completing work"))
				{
					task.Wait();
				}
			}
		}

		public void Dispose()
		{
			worker.Abort();
		}
	}
}