using Moq;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
	public static class MockExtensions
	{
		public static void SetupGet(this Mock<IGitHubClient> github, Repository repo, Issue issue)
		{
			github.Setup(x => x.Issue.Get(repo.Owner.Login, repo.Name, issue.Number))
				.Returns(Task.FromResult(issue));
		}

		public static void SetupGet(this Mock<IGitHubClient> github, string owner, string name, Issue issue)
		{
			github.Setup(x => x.Issue.Get(owner, name, issue.Number))
				.Returns(Task.FromResult(issue));
		}

		public static void SetupSearch(this Mock<IGitHubClient> github, params Issue[] result)
		{
			github.Setup(x => x.Search.SearchIssues(It.IsAny<SearchIssuesRequest>()))
				.Returns(Task.FromResult(new SearchIssuesResult
				{
					Items = result.ToList(), 
					TotalCount = result.Length
				}));
		}

		public static void SetupSearch(this Mock<IGitHubClient> github, ItemState state, params Issue[] result)
		{
			github.Setup(x => x.Search.SearchIssues(It.Is<SearchIssuesRequest>(s => s.State == state)))
				.Returns(Task.FromResult(new SearchIssuesResult
				{
					Items = result.ToList(), 
					TotalCount = result.Length
				}));
		}
	}
}
