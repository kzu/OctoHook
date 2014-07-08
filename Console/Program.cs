namespace OctoHook
{
	using Microsoft.AspNet.SignalR.Client;
	using OctoHook.Diagnostics;
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;

    class Program
    {
        static readonly ITracer tracer = Tracer.Get(typeof(Program));
        const string TracerHubUrl = "http://tracer.azurewebsites.net/";
        const string HubName = "Tracer";

        static void Main(string[] args)
        {
            Tracer.Initialize(new TracerManager());

            tracer.Info("Starting TracerHub Console");

            if (args.Length != 1)
            {
                Console.WriteLine("Usage: " + typeof(Program).Assembly.ManifestModule.FullyQualifiedName + " groupName");
                Console.WriteLine("Press 'Q' to exit.");
                Console.ReadLine();
                return;
            }

            var data = new Dictionary<string, string>
            {
                { "groupName", args[0] }
            };

            using (var hub = new HubConnection(TracerHubUrl, data))
            {
                IHubProxy proxy = hub.CreateHubProxy(HubName);
                IDisposable handler = proxy.On<TraceEvent>("TraceEvent", trace => Tracer.Get(trace.Source).Trace(trace.EventType, trace.Message));

#if DEBUG
                //hub.TraceLevel = TraceLevels.All;
                //hub.TraceWriter = Console.Out;
#endif

                hub.Start().Wait();

                Console.WriteLine("Send trace event:  [E(rror)|I(nformation)|W(arning)]:[Source]:[Message]");
                Console.WriteLine("Set tracing level: [Source]=[Off|Critical|Error|Warning|Information|Verbose|All]");
                Console.WriteLine("Press 'Q' to exit.");
                var line = Console.ReadLine();

                while (!line.Equals("Q", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (line.IndexOf(':') != -1)
                    {
                        var trace = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (trace.Length == 3)
                        {
                            var type = TraceEventType.Information;
                            switch (trace[0])
                            {
                                case "E":
                                    type = TraceEventType.Error;
                                    break;
                                case "W":
                                    type = TraceEventType.Warning;
                                    break;
                                default:
                                    break;
                            }

                            proxy.Invoke("TraceEvent", new TraceEvent
                            {
                                EventType = type,
                                Source = trace[1],
                                Message = trace[2],
                            });
                        }
                        else
                        {
                            Console.WriteLine("Send trace event:  [E(rror)|I(nformation)|W(arning)]:[Source]:[Message]");
                        }
                    }
                    else if (line.IndexOf('=') != -1)
                    {
                        var trace = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        SourceLevels level;
                        if (trace.Length == 2 && Enum.TryParse<SourceLevels>(trace[1], out level))
                        {
                            proxy.Invoke("SetTracingLevel", trace[0], level);
                        }
                        else
                        {
                            Console.WriteLine("Set tracing level: [Source]=[Off|Critical|Error|Warning|Information|Verbose|All]");
                        }
                    }

                    line = Console.ReadLine();
                }
            }
        }
    }
}
