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
            try
            {
                LoggerFacade.UseILogger(log);
                LoggerFacade.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                LoggerFacade.LogInformation("In Run.");

                //LogEnvStrings();
                ConfigValues.Initialize();
                CheckIfPageChanged();

                //string shareName = ConfigValues.GetValue("shareName");
                //string dirName = ConfigValues.GetValue("dirName");
                //string fileName = ConfigValues.GetValue("lastChangedFileName");

                //var azureFiles = new AzureFileShare(shareName, dirName);
                //Task<bool> task = azureFiles.WriteToFile(fileName, "Jan 29, 2021");
                //LoggerFacade.LogInformation($"Finished write, result={task.Result}.");

                //Task<string> lastMod = azureFiles.ReadFile(fileName);
                //LoggerFacade.LogInformation($"Finished read, contents = {lastMod.Result}");

                //SendMsg_Succeeds();
                            
                //string myUrl = @"https://www.canada.ca/en/public-health/services/diseases/2019-novel-coronavirus-infection/prevention-risks/covid-19-vaccine-treatment/vaccine-rollout.html";
                //string lastDate = GetLastModifiedDate(myUrl);
                //LoggerFacade.LogInformation($"lastModified={lastDate}.");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in TimerTriggerCSharp1.Run.");
            }

        }

        public static async void CheckIfPageChanged()
        {
            try
            {
                LoggerFacade.LogInformation("Starting CheckIfPageChanged.");

                //LogEnvStrings();
                ConfigValues.Initialize();

                // Read in html for given page.
                string url = ConfigValues.GetValue("webSiteUrl");
                string pageText = GetPageText(url);
                // Parse out last changed date.
                string lastChangedDate = GetChangedDate(pageText);
                
                // Compare to date from last time checked page.
                // If different:
                //      Save new date to check against last time.
                //      Send message that page changed.
                PageChangeTracker chgTracker = new PageChangeTracker();
                bool dateChanged = false;
                if (chgTracker.HasDateChanged(lastChangedDate))
                {
                    chgTracker.SaveChangeDate(lastChangedDate);
                    var result = await SendMessage(lastChangedDate, url);
                    if (result)
                    {
                        dateChanged = true;
                    }
                }
                LoggerFacade.LogInformation($"Finished CheckIfPageChanged, dateChanged={dateChanged}.");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in TimerTriggerCSharp1.CheckIfPageChanged.");
            }

        }

        // Return html from requested page.
        public static string GetPageText(string url)
        {
            LoggerFacade.LogInformation("Starting GetPageText.");
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream data = response.GetResponseStream();
            string htmlResponse = "";
            using (StreamReader sr = new StreamReader(data))
            {
                htmlResponse = sr.ReadToEnd();
            }
            //LoggerFacade.LogInformation($"response={htmlResponse}");
            LoggerFacade.LogInformation("Finished GetPageText.");

            return htmlResponse;
        }

        // Parse out the last changed date from the given text.
        public static string GetChangedDate(string htmlResponse)
        {
            // Searching from end because target is at bottom of page.
            string target = "dateModified";
            int startLoc = htmlResponse.LastIndexOf(target);
            string lastModified = htmlResponse.Substring(startLoc + target.Length + 2, 10);
            LoggerFacade.LogInformation($"GetChangedDate, dateModified={lastModified}");
            
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

        private static async Task<bool> SendMessage(string changeDate, string url)
        {
            bool sentOk = false;
            try
            {
                Notification sns = new Notification();
                string topic = ConfigValues.GetValue("snsTopic");
                LoggerFacade.LogInformation($"topic={topic}");
                string msg = $"The web page {url} changed, " + 
                                $"new last date changed is {changeDate}";
                sentOk = await sns.SendSnsMessage(topic, msg);
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in SendMsg_Succeeds");
            }

            return sentOk;
        }

    }
}
