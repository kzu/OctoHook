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
	using System;
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

			var tracingLevel = ConfigurationManager.AppSettings["TracingLevel"];
			SourceLevels sourceLevel = SourceLevels.Information;
			Enum.TryParse<SourceLevels>(tracingLevel, out sourceLevel);

			manager.SetTracingLevel("*", sourceLevel);
			manager.AddListener("*", new RealtimeTraceListener(ConfigurationManager.AppSettings["TracerHub"]));

			Tracer.Initialize(manager);

			GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(
				ContainerConfiguration.Configure(queue));

			Tracer.Get<WebApiApplication>().Info("{0} Version {1}",
				Assembly.GetExecutingAssembly().GetName().Name,
				Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

			GlobalConfiguration.Configure(WebApiConfig.Register);
		}
	}
}
