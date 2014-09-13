namespace OctoHook.Web
{
    using Autofac;
    using OctoHook.CommonComposition;
    using Octokit;
    using Octokit.Internal;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Reflection;

	public static class ContainerConfiguration
	{
		public static IContainer Configure(string authToken, IEnumerable<Assembly> assemblies)
		{
			return Configure(authToken, assemblies.ToArray());
		}

		public static IContainer Configure(string authToken, params Assembly[] assemblies)
		{
			IContainer container = null;

			var builder = new ContainerBuilder();

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
						new Credentials(authToken))));

            builder.Register<IApiConnection>(c =>
                new ApiConnection(c.Resolve<IGitHubClient>().Connection));

			container = builder.Build();

			return container;
		}
	}
}