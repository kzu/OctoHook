namespace Tests
{
	using Autofac.Extras.CommonServiceLocator;
	using Moq;
	using OctoHook;
	using OctoHook.WebHooks;
	using Octokit.Events;
	using System.Linq;
	using Xunit;

	public class CompositionTests
	{
		[Fact]
		public void when_retrieving_issue_hooks_then_retrieves_components()
		{
			var container = ContainerConfiguration.Configure(Mock.Of<IWorkQueue>());
			var locator = new AutofacServiceLocator(container);

			var hooks = locator.GetAllInstances<IWebHook<IssuesEvent>>().ToList();

			Assert.True(hooks.Any(h => h.GetType() == typeof(AutoLink)));
		}
	}
}
