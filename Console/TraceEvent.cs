namespace OctoHook
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Payload data to send to the hub about the traced event.
    /// </summary>
    partial class TraceEvent
    {
        /// <summary>
        /// Gets or sets the type of the event trace.
        /// </summary>
        public TraceEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the trace message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the source of the trace event.
        /// </summary>
        public string Source { get; set; }
    }}
