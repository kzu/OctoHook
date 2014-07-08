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

		const int maxRetries = 5;

		BlockingCollection<WorkEntry> queue = new BlockingCollection<WorkEntry>();
		Thread worker;

		public WorkQueue()
		{
			tracer.Verbose("Started new worker queue.");
			worker = new Thread(ProcessWork);
			worker.Start();
		}

		public void Queue(Action work, string description)
		{
			queue.Add(new WorkEntry(work, description));
		}

		private void ProcessWork()
		{
			foreach (var work in queue.GetConsumingEnumerable())
			{
				try
				{
					tracer.Verbose("Atempt {0}: {1}.", work.Retries++, work.Description);
					work.Action();
					tracer.Verbose("Successfully run: {2}.", work.Description);
				}
				catch (Exception ex)
				{
					if (work.Retries < 5)
					{
						tracer.Warn("Failed to run: {0}. Retrying later.", work.Description);
						queue.Add(work);
					}
					else
					{
						tracer.Error(ex, "Failed to run: {0}. Discarding work.", work.Description);
					}
				}
			}
		}

		public void Dispose()
		{
			worker.Abort();
		}

		private class WorkEntry
		{
			public WorkEntry(Action action, string description)
			{
				this.Action = action;
				this.Description = description;
			}

			public Action Action { get; private set; }
			public string Description { get; private set; }
			public int Retries { get; set; }
		}
	}
}