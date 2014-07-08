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

	public class GitHubController : ApiController
	{
		static readonly ITracer tracer = Tracer.Get<GitHubController>();

		IServiceLocator locator;
		IWorkQueue work;

		public GitHubController(IServiceLocator locator, IWorkQueue work)
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

			tracer.Verbose("Received GitHub callback for event of type '{0}'.", type);

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

		private void Process<TEvent>(TEvent @event)
		{
			foreach (var hook in locator.GetAllInstances<IWebHook<TEvent>>().AsParallel())
			{
				tracer.Verbose("Queuing process with '{0}' hook.", hook.GetType().Name);
				work.Queue(() => hook.Process(@event), hook.Describe(@event));
			}
		}
	}
}