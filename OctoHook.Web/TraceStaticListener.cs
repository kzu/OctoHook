namespace OctoHook.Web
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Web;

    internal class TraceStaticListener : TraceListener
    {
        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            TraceEvent(null, "Trace", TraceEventType.Information, 0, message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            TraceEvent(eventCache, source, eventType, id,
                args == null ? format : string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if ((base.Filter == null) || base.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                var prefix = source;
                var dotIndex = prefix.LastIndexOf('.');
                if (dotIndex != -1)
                    prefix = prefix.Substring(dotIndex + 1);

                switch (eventType)
                {
                    case TraceEventType.Critical:
                    case TraceEventType.Error:
                        Trace.TraceError("[{0}] {1}", prefix, message);
                        break;
                    case TraceEventType.Verbose:
                    case TraceEventType.Information:
                        Trace.TraceInformation("[{0}] {1}", prefix, message);
                        break;
                    case TraceEventType.Warning:
                        Trace.TraceWarning("[{0}] {1}", prefix, message);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}