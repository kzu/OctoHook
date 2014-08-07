namespace OctoHook.Controllers
{
	using Autofac;
	using Microsoft.Practices.ServiceLocation;
	using Newtonsoft.Json.Linq;
	using System.Collections.Generic;
	using System.Net.Http;
	using System.Reflection;
	using System.Web.Http;
	using System.Web.Http.Results;
	using System.Linq;
	using Octokit.Events;
	using Newtonsoft.Json;
	using OctoHook.Diagnostics;
	using System;

	public class GitHubController : ApiController
	{
		static readonly ITracer tracer = Tracer.Get<GitHubController>();

		IServiceLocator locator;
		IJobQueue work;

		public GitHubController(IServiceLocator locator, IJobQueue work)
		{
			this.locator = locator;
			this.work = work;
		}

		public string Get()
		{
			return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
		}

		public void Post(HttpRequestMessage request, [FromBody]JObject json)
		{
			var type = string.Empty;
			IEnumerable<string> keys = null;
			if (!request.Headers.TryGetValues("X-GitHub-Event", out keys))
				return;

			type = keys.First();

			System.Diagnostics.Trace.TraceInformation(json.ToString(Formatting.Indented));

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
			foreach (var hook in locator.GetAllInstances<IOctoJob<TEvent>>())
			{
				tracer.Verbose("Queuing process with '{0}' job.", hook.GetType().Name);
				work.Queue(() => hook.ProcessAsync(@event));
			}

			// Synchronously execute hooks
			foreach (var hook in locator.GetAllInstances<IOctoHook<TEvent>>().AsParallel())
			{
				tracer.Verbose("Processing with '{0}' hook.", hook.GetType().Name);
				hook.Process(@event);
			}
		}
	}
}