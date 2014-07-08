namespace Octokit.Events
{
	using Newtonsoft.Json;
	using Octokit;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Web;

	public class PushEvent
	{
		public class CommitInfo
		{
			[JsonProperty("id")]
			public string Sha { get; set; }
			public bool Distinct { get; set; }
			public string Message { get; set; }
			public Uri Url { get; set; }
			public UserInfo Author { get; set; }
			public UserInfo Committer { get; set; }
			public string[] Added { get; set; }
			public string[] Removed { get; set; }
			public string[] Modified { get; set; }
		}

		public class UserInfo
		{
			public string Name { get; set; }
			public string Email { get; set; }
			public string UserName { get; set; }
		}

		public string Ref { get; set; }
		public string After { get; set; }
		public string Before { get; set; }
		public bool Created { get; set; }
		public bool Deleted { get; set; }
		public bool Forced { get; set; }
		public string Compare { get; set; }
		public CommitInfo[] Commits { get; set; }
		[JsonProperty("head_commit")]
		public CommitInfo HeadCommit { get; set; }
		public Repository Repository { get; set; }
		public UserInfo Pusher { get; set; }
	}
}