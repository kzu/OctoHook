namespace OctoHook.Web.Controllers
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Web;
    using System.Web.Http;
    using OctoHook.Web;

	public class GitHubController : ApiController
	{
        static readonly SourceLevels traceLevel;

		static GitHubController()
		{
			var tracingLevel = ConfigurationManager.AppSettings["TracingLevel"];
			SourceLevels sourceLevel = SourceLevels.Information;
			Enum.TryParse<SourceLevels>(tracingLevel, out sourceLevel);

            traceLevel = sourceLevel;
		}

		public string Get()
		{
            return new OctoController(
                ConfigurationManager.AppSettings["GitHubToken"],
                traceLevel, 
                HttpContext.Current.ApplicationInstance)
                .Get();
		}

		public void Post(HttpRequestMessage request, [FromBody]JObject json)
		{
            new OctoController(
                ConfigurationManager.AppSettings["GitHubToken"],
                traceLevel, 
                HttpContext.Current.ApplicationInstance)
                .Post(request, json);
		}
	}
}