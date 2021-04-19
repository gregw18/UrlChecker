using System;

using Microsoft.Extensions.Logging;

namespace GAWUrlChecker
{
    // Read in and provide access to config values for function.
    // Names of config values are hardcodes, as are whether they
    // are secrets - i.e. whether they are found in the key vault or in
    // environment variables.
    // Data stored in a dictionary of strings - if value isn't a string,
    // caller has to convert it.
    public static class LoggerFacade
    {

        private static ILogger iLoggerInstance;
        private static bool useILogger = false;
        private static bool useConsole = false;

        public static void UseConsole()
        {
            if (!useILogger)
            {
                useConsole = true;
            }
            else
            {
                throw new ArgumentException("LoggerFacade already configured to use ILogger");
            }
        }

        public static void UseILogger(ILogger myLog)
        {
            if (!useConsole)
            {
                iLoggerInstance = myLog;
                useILogger = true;
            }
            else
            {
                throw new ArgumentException("LoggerFacade already configured to use Console");
            }
        }

        public static void LogInformation(string logMsg)
        {
            if (useILogger)
            {
                WriteToILogger(logMsg);
            }
            else if (useConsole)
            {
                WriteToConsole(logMsg);
            }
            else
            {
                WriteToConsole(logMsg);
            }
        }

        private static void WriteToILogger(string logMsg)
        {
            iLoggerInstance.LogInformation(logMsg);
        }

        private static void WriteToConsole(string logMsg)
        {
            Console.WriteLine(logMsg);
        }
        
    }
}
