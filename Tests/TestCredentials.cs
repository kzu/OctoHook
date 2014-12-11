using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace Tests
{
	public static class TestCredentials
	{
		public static Credentials Create ()
		{
			var token = File.ReadAllText (@"..\..\Token");
			if (string.IsNullOrEmpty (token))
				token = Environment.GetEnvironmentVariable ("OCTOHOOK");

			if (string.IsNullOrEmpty (token))
				return null;

			return new Credentials (token);
		}
	}
}
