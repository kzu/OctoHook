namespace OctoHook
{
	using Autofac;
	using Autofac.Extras.CommonServiceLocator;
	using Autofac.Integration.WebApi;
	using Microsoft.Practices.ServiceLocation;
	using OctoHook.CommonComposition;
	using OctoHook.Diagnostics;
	using Octokit;
	using Octokit.Internal;
	using System.Configuration;
	using System.Diagnostics;
	using System.Reflection;
	using System.Web;
	using System.Web.Http;

	public class WebApiApplication : HttpApplication
	{
		static WorkQueue queue = new WorkQueue();

		protected void Application_Start()
		{
			var manager = new TracerManager();
			manager.SetTracingLevel("*", SourceLevels.All);
			manager.AddListener("*", new RealtimeTraceListener("OctoHook"));

			Tracer.Initialize(manager);

			GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(
				ContainerConfiguration.Configure(queue));

			Tracer.Get<WebApiApplication>().Info("Starting web application...");
			Tracer.Get<WebApiApplication>().Info(
				Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

			GlobalConfiguration.Configure(WebApiConfig.Register);
		}
	}
}
