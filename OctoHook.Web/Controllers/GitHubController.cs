namespace OctoHook.Web
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Web;
    using System.Web.Http;

	public class GitHubController : ApiController
	{
        static readonly SourceLevels traceLevel;
        private OctoController controller;

		static GitHubController()
		{
			var tracingLevel = ConfigurationManager.AppSettings["TracingLevel"];
			SourceLevels sourceLevel = SourceLevels.Information;
			Enum.TryParse<SourceLevels>(tracingLevel, out sourceLevel);

            traceLevel = sourceLevel;
		}

        public GitHubController()
        {
            this.controller = new OctoController(
                ConfigurationManager.AppSettings["GitHubToken"],
                traceLevel, 
                HttpContext.Current.ApplicationInstance);
        }

		public string Get()
		{
            return controller.Get();
		}

		public void Post(HttpRequestMessage request, [FromBody]JObject json)
		{
            controller.Post(request, json);
		}
	}
}