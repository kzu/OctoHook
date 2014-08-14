namespace OctoHook.Web
{
    using Autofac.Extras.CommonServiceLocator;
    using Microsoft.Practices.ServiceLocation;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using OctoHook.Diagnostics;
    using Octokit.Events;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Web;

    public class OctoController
    {
        static readonly object syncLock = new object();
        static ITracer tracer;
        static IServiceLocator components;

        IJobQueue work;

        internal OctoController(string authToken, SourceLevels traceLevel)
        {
            Initialize(authToken, traceLevel, null);
        }

        public OctoController(string authToken, SourceLevels traceLevel, HttpApplication webApp)
        {
            Guard.NotNull(() => webApp, webApp);
            Initialize(authToken, traceLevel, webApp);
        }

        private void Initialize(string authToken, SourceLevels traceLevel, HttpApplication webApp)
        {
            InitializeTracing(traceLevel);
            InitializeComposition(authToken, webApp);

            this.work = components.GetInstance<IJobQueue>();
        }

        public string Get()
        {
            return Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
        }

        public void Post(HttpRequestMessage request, JObject json)
        {
            var type = string.Empty;
            IEnumerable<string> keys = null;
            if (!request.Headers.TryGetValues("X-GitHub-Event", out keys))
                return;

            type = keys.First();

            tracer.Verbose("Received GitHub webhook callback for event of type '{0}'.", type);

            try
            {
                switch (type)
                {
                    case "issues":
                        Process<IssuesEvent>(JsonConvert.DeserializeObject<IssuesEvent>(json.ToString()));
                        break;
                    case "push":
                        Process<PushEvent>(JsonConvert.DeserializeObject<PushEvent>(json.ToString()));
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                tracer.Error(@"Failed to process request: {0}.
--- Exception ---
{1}
--- Request ---
{2}", e.Message, e, json.ToString(Formatting.Indented));
                throw;
            }
        }

        private void Process<TEvent>(TEvent @event)
        {
            // Queue async/background jobs
            foreach (var hook in components.GetAllInstances<IOctoJob<TEvent>>())
            {
                tracer.Verbose("Queuing process with '{0}' job.", hook.GetType().Name);
                work.Queue(() => hook.ProcessAsync(@event));
            }

            // Synchronously execute hooks
            foreach (var hook in components.GetAllInstances<IOctoHook<TEvent>>().AsParallel())
            {
                tracer.Verbose("Processing with '{0}' hook.", hook.GetType().Name);
                hook.Process(@event);
            }
        }

        private static void InitializeComposition(string authToken, HttpApplication webApp)
        {
            if (components != null)
                return;

            lock (syncLock)
            {
                if (components != null)
                    return;

                var assemblies = new HashSet<Assembly>();
                assemblies.Add(Assembly.GetExecutingAssembly());

                // By default, current dir. This makes it work for unit tests too.
                var baseDir = "";
                if (webApp != null)
                    baseDir = webApp.Server.MapPath("bin");

                foreach (var file in Directory.EnumerateFiles(baseDir, "*.dll"))
                {
                    tracer.Verbose("Loading {0} for composition.", Path.GetFileName(file));
                    try
                    {
                        var name = AssemblyName.GetAssemblyName(file);

                        try
                        {
                            var asm = Assembly.Load(name);
                            if (!assemblies.Contains(asm))
                                assemblies.Add(asm);
                        }
                        catch (Exception ex)
                        {
                            tracer.Warn(ex, "Failed to load {0} for composition.", Path.GetFileName(file));
                        }
                    }
                    catch { } // AssemblyName loading could fail for non-managed assemblies
                }

                components = new AutofacServiceLocator(ContainerConfiguration.Configure(authToken, assemblies));
            }
        }

        private static void InitializeTracing(SourceLevels traceLevel)
        {
            if (tracer != null)
                return;

            lock (syncLock)
            {
                if (tracer != null)
                    return;

                var manager = new TracerManager();

                manager.SetTracingLevel("*", traceLevel);
                manager.AddListener("*", new TraceStaticListener());

                Tracer.Initialize(manager);

                tracer = Tracer.Get<OctoController>();
            }
        }
    }
}