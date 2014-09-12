namespace OctoHook.Web
{
    using Autofac;
    using Autofac.Core.Lifetime;
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
        static IContainer components;

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

            this.work = components.Resolve<IJobQueue>();
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
            var hooks = new HashSet<string>(request.GetQueryNameValuePairs()
                .Where(pair => pair.Key == "h" || pair.Key == "hooks")
                .SelectMany(pair => pair.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)));

            tracer.Verbose("Received GitHub webhook callback for event of type '{0}'.", type);
            if (hooks.Count > 0)
                tracer.Verbose("Received specific hooks to process: {0}.", string.Join(", ", hooks));

            try
            {
                switch (type)
                {
                    case "issues":
                        Process<IssuesEvent>(JsonConvert.DeserializeObject<IssuesEvent>(json.ToString()), hooks);
                        break;
                    case "push":
                        Process<PushEvent>(JsonConvert.DeserializeObject<PushEvent>(json.ToString()), hooks);
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

        private void Process<TEvent>(TEvent @event, HashSet<string> hooks)
        {
            Func<string, bool> shouldProcess;
            if (hooks.Count == 0)
                shouldProcess = _ => true;
            else
                shouldProcess = hook => hooks.Contains(hook);

            using (var scope = components.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag))
            {
                // Queue async/background jobs
                foreach (var job in scope.Resolve<IEnumerable<IOctoJob<TEvent>>>())
                {
                    var jobName = job.GetType().Name;
                    if (shouldProcess(jobName))
                    {
                        tracer.Verbose("Queuing process with '{0}' job.", jobName);
                        work.Queue(() => job.ProcessAsync(@event));
                    }
                    else
                    {
                        tracer.Verbose("Skipping process with '{0}' job since it was not in the explicit hook list received.");
                    }
                }

                // Synchronously execute hooks
                foreach (var hook in scope.Resolve<IEnumerable<IOctoHook<TEvent>>>().AsParallel())
                {
                    var hookName = hook.GetType().Name;
                    if (shouldProcess(hookName))
                    {
                        tracer.Verbose("Processing with '{0}' hook.", hookName);
                        hook.Process(@event);
                    }
                    else
                    {
                        tracer.Verbose("Skipping process with '{0}' hook since it was not in the explicit hook list received.");
                    }
                }
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
                var baseDir = ".";
                if (webApp != null)
                    baseDir = webApp.Server.MapPath("/bin");

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

                components = ContainerConfiguration.Configure(authToken, assemblies);
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