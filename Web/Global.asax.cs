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
	using System.Collections.Generic;
	using System.Configuration;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.Web;
	using System.Web.Http;

	public class WebApiApplication : HttpApplication
	{
		protected void Application_Start()
		{
			var manager = new TracerManager();

			var tracingLevel = ConfigurationManager.AppSettings["TracingLevel"];
			SourceLevels sourceLevel = SourceLevels.Information;
			Enum.TryParse<SourceLevels>(tracingLevel, out sourceLevel);

			manager.SetTracingLevel("*", sourceLevel);
			manager.AddListener("*", new TraceStaticListener());

			Tracer.Initialize(manager);

			var tracer = Tracer.Get<WebApiApplication>();

			var assemblies = new HashSet<Assembly>();
			assemblies.Add(Assembly.GetExecutingAssembly());
			foreach (var file in Directory.EnumerateFiles(Server.MapPath("bin"), "*.dll"))
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

			GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(
				ContainerConfiguration.Configure(assemblies));

			tracer.Info("{0} Version {1}",
				Assembly.GetExecutingAssembly().GetName().Name,
				Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

			GlobalConfiguration.Configure(WebApiConfig.Register);
		}
	}
}
