namespace OctoHook
{
	using Autofac;
	using Autofac.Extras.CommonServiceLocator;
	using Autofac.Integration.WebApi;
	using Microsoft.Practices.ServiceLocation;
	using OctoHook.CommonComposition;
	using Octokit;
	using Octokit.Internal;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Reflection;
	using System.Web;
	using System.Web.Http;
	using System.Linq;

	public static class ContainerConfiguration
	{
		public static IContainer Configure(IJobQueue queue)
		{
			return Configure(queue, typeof(ContainerConfiguration).Assembly);
		}

		public static IContainer Configure(IJobQueue queue, IEnumerable<Assembly> assemblies)
		{
			return Configure(queue, assemblies.ToArray());
		}

		public static IContainer Configure(IJobQueue queue, params Assembly[] assemblies)
		{
			IContainer container = null;

			var builder = new ContainerBuilder();

			// Additional extensibility hook, where components can also 
			// provide WebApi controllers.
			builder.RegisterApiControllers(assemblies);
			builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

			builder.RegisterComponents(assemblies)
				// Non-singleton components are registered as per-request.
				.ActivatorData.ConfigurationActions.Add((t, rb) =>
				{
					// We know we have the ComponentAttribute since this is the result of RegisterComponents.
					if (!rb.ActivatorData.ImplementationType.GetCustomAttributes(true).OfType<ComponentAttribute>().First().IsSingleton)
						rb.InstancePerRequest();
				});

			builder.Register<IGitHubClient>(c =>
				new GitHubClient(
					new ProductHeaderValue("OctoHook"),
					new InMemoryCredentialStore(
						new Credentials(ConfigurationManager.AppSettings["GitHubToken"]))));

			builder.Register<IServiceLocator>(c =>
					new AutofacServiceLocator(c.Resolve<IComponentContext>()))
				.InstancePerRequest();

			container = builder.Build();

			return container;
		}
	}
}