namespace OctoHook.Web
{
    using OctoHook.CommonComposition;
    using OctoHook.Diagnostics;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;

    [Component(IsSingleton = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class JobQueue : IJobQueue, IDisposable
    {
        static readonly ITracer tracer = Tracer.Get<JobQueue>();

        BlockingCollection<Func<Task>> queue = new BlockingCollection<Func<Task>>();
        Thread worker;

        public JobQueue()
        {
            tracer.Verbose("Started new worker queue.");
            worker = new Thread(ProcessWork);
            worker.Start();
        }

        public void Queue(Func<Task> work)
        {
            queue.Add(work);
        }

        private void ProcessWork()
        {
            foreach (var work in queue.GetConsumingEnumerable())
            {
                try
                {
                    work.Invoke().Wait();
                }
                catch (AggregateException ae)
                {
                    tracer.Error(ae.GetBaseException());
                }
                catch (Exception ex)
                {
                    tracer.Error(ex);
                }
            }
        }

        public void Dispose()
        {
            worker.Abort();
        }
    }
}