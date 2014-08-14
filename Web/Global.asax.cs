namespace OctoHook
{
    using System.Web;
	using System.Web.Http;

	public class WebApplication : HttpApplication
	{
		protected void Application_Start()
		{			
			GlobalConfiguration.Configure(WebApiConfig.Register);
		}
	}
}
