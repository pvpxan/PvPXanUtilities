using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StreamlineUtilities
{
    public interface ILogStreamline
    {
        void LogEntry(string log);
        void Exception(string log, Exception ex);
    }

    public static class LogStreamline
    {
        private static ILogStreamline _ILogHTTP = null;
        public static void SetLogStreamline(ILogStreamline logHTTP)
        {
            lock (_ILogHTTP)
            {
                _ILogHTTP = logHTTP;
            }
        }

        private static bool _LogOutput = true;
        public static bool LogOutput
        {
            get
            {
                return _LogOutput;
            }
        }
        public static void SetOutputLogging(bool state)
        {
            _LogOutput = state;
        }

        public static void LogEntry(string log)
        {
            if (_ILogHTTP == null || LogOutput == false)
            {
                return;
            }

            _ILogHTTP.LogEntry(log);
        }

        public static void Exception(string log, Exception Ex)
        {
            if (_ILogHTTP == null || LogOutput == false)
            {
                return;
            }

            _ILogHTTP.Exception(log, Ex);
        }
    }
}
