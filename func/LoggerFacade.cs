using System;

using Microsoft.Extensions.Logging;

namespace GAWUrlChecker
{
    // Facade for Logging. Can log to ILogger or console. Intended to be called directly
    // from anywhere. Needs to be initialized before first call - defaults to console if not.
    // Currently, all log levels are treated same - everything logged all the time.
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

        public static void LogError(Exception ex, string logMsg)
        {
            string fullMsg = "Error: " + logMsg + "\n" + ex.ToString();
            LogInformation(fullMsg);
        }

        public static void LogError(string logMsg)
        {
            string fullMsg = "Error: " + logMsg;
            LogInformation(fullMsg);
        }

        public static void LogDebug(Exception ex, string logMsg)
        {
            LogInformation(logMsg);
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
