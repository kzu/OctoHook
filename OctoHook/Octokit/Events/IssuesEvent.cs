namespace Octokit.Events
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Web;

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class IssuesEvent
    {
        public enum IssueAction
        {
            Opened,
            Closed,
            Reopened,
			Assigned,
			Unassigned,
			Labeled,
			Unlabeled,
        }

        public IssueAction Action { get; set; }

        public Issue Issue { get; set; }

        public Repository Repository { get; set; }

        public User Sender { get; set; }

        internal string DebuggerDisplay
        {
            get
            {
                return string.Format("{0}/{1}/issues/{2} {3} by {4}",
                    Repository.Owner, Repository.Name, Issue.Number, Action, Sender.Login);
            }
        }
    }
}