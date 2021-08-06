using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StreamlineUtilities
{
    public interface ILogUtilities
    {
        void LogEntry(string log);
        void Exception(string log, Exception ex);
    }

    public static class LogUtilities
    {
        private static ILogUtilities _ILogUtilities = new LogDependency();
        private static bool interfaceImplemented = false;

        public static void SetLogControl(ILogUtilities logUtilities)
        {
            lock (_ILogUtilities)
            {
                _ILogUtilities = logUtilities;
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
            if (interfaceImplemented == false || LogOutput == false)
            {
                return;
            }

            _ILogUtilities.LogEntry(log);
        }

        public static void Exception(string log, Exception Ex)
        {
            if (interfaceImplemented == false || LogOutput == false)
            {
                return;
            }

            _ILogUtilities.Exception(log, Ex);
        }

        // Hack: Place holder to allow a static class to hold a static interface reference.
        private class LogDependency : ILogUtilities
        {
            public void LogEntry(string log)
            {

            }

            public void Exception(string log, Exception Ex)
            {

            }
        }
    }
}
