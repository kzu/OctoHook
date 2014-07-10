namespace OctoHook
{
	using Autofac;
	using Autofac.Extras.CommonServiceLocator;
	using Autofac.Integration.WebApi;
	using Microsoft.Practices.ServiceLocation;
	using OctoHook.CommonComposition;
	using Octokit;
	using Octokit.Internal;
	using System.Configuration;
	using System.Reflection;
	using System.Web;
	using System.Web.Http;

	public static class ContainerConfiguration
	{
		public static IContainer Configure(IWorkQueue queue)
		{
			IContainer container = null;

			var builder = new ContainerBuilder();
			builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
			builder.RegisterComponents(Assembly.GetExecutingAssembly())
				.InstancePerRequest();

			builder.Register<IGitHubClient>(c =>
				new GitHubClient(
					new ProductHeaderValue("OctoHook"),
					new InMemoryCredentialStore(
						new Credentials(ConfigurationManager.AppSettings["GitHubToken"]))));

			builder.Register<IServiceLocator>(c => new AutofacServiceLocator(container));
			builder.RegisterInstance(queue).SingleInstance();
			container = builder.Build();

			return container;
		}
	}
}