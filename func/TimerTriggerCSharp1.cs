using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;


namespace GAWUrlChecker
{
    public static class TimerTriggerCSharp1
    {
        [FunctionName("TimerTriggerCSharp1")]
        public static void Run([TimerTrigger("0 0 5 * * *")]TimerInfo myTimer, 
                                ILogger log)
        {
            LoggerFacade.UseILogger(log);
            LoggerFacade.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            LoggerFacade.LogInformation("In Run.");

            LogEnvStrings();
            ConfigValues.Initialize();

            string shareName = ConfigValues.GetValue("shareName");
            string dirName = ConfigValues.GetValue("dirName");
            string fileName = ConfigValues.GetValue("lastChangedFileName");

            var azureFiles = new AzureFileShare(shareName, dirName);
            Task<bool> task = azureFiles.WriteToFile(fileName, "Jan 29, 2021");
            LoggerFacade.LogInformation($"Finished write, result={task.Result}.");

            Task<string> lastMod = azureFiles.ReadFile(fileName);
            LoggerFacade.LogInformation($"Finished read, contents = {lastMod.Result}");
            //string myUrl = @"https://www.canada.ca/en/public-health/services/diseases/2019-novel-coronavirus-infection/prevention-risks/covid-19-vaccine-treatment/vaccine-rollout.html";
            //string lastDate = GetLastModifiedDate(myUrl);
            //LoggerFacade.LogInformation($"lastModified={lastDate}.");

        }

        public static string GetLastModifiedDate(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream data = response.GetResponseStream();
            string htmlResponse = "";
            using (StreamReader sr = new StreamReader(data))
            {
                htmlResponse = sr.ReadToEnd();
            }
            //LoggerFacade.LogInformation($"response={htmlResponse}");

            // Searching from end because target is at bottom of page.
            string target = "dateModified";
            int startLoc = htmlResponse.LastIndexOf(target);
            string lastModified = htmlResponse.Substring(startLoc + target.Length + 2, 10);
            LoggerFacade.LogInformation($"dateModified={lastModified}");
            
            return lastModified;
        }

        public static void LogEnvStrings()
        {
            var envStrings = System.Environment.GetEnvironmentVariables();
            var sortedEnv = new SortedList(envStrings);
            LoggerFacade.LogInformation("\nEnvironment variables");
            foreach (string s in sortedEnv.Keys)
            {
                LoggerFacade.LogInformation( $"key: {s}, value:{envStrings[s]}");
            }
            LoggerFacade.LogInformation("--------\n");
        }

    }
}
