using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;


// Azure function, timer triggered, that checks whether a given web page has changed
// since the last time it was checked. If yes, sends an email to that effect.
// Uses dateModified property that happens to be in the web page that I'm interested in
// to tell whether the page has changed. Uses AWS SNS to send the email, as Azure doesn't
// appear to have service for sending emails, and I thought it would be interesting to see
// what it takes to get Azure and AWS to work together.
// Automatically publishes local settings to the azure function, for most configuration,
// but also stores some secrets in an Azure Key Vault - currently the AWS access keys.
// Stores date the page was last changed in an Azure Storage file share.

namespace GAWUrlChecker
{
    public static class TimerTriggerCSharp1
    {
        // Timer runs at 5am and 11am every day, eastern standard
        // (Cron expression uses UTC.)
        [FunctionName("TimerTriggerCSharp1")]
        public static async Task Run([TimerTrigger("0 0 10,16 * * *")]TimerInfo myTimer, 
                                ILogger log)
        {
            try
            {
                LoggerFacade.UseILogger(log);
                LoggerFacade.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                LoggerFacade.LogInformation("In Run.");

                // LogEnvStrings();
                string url = ConfigValues.GetValue("webSiteUrl");
                string fileName = ConfigValues.GetValue("lastChangedFileName");
                await DidPageChange(url, fileName);
                LoggerFacade.LogInformation("Finished Run.");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in TimerTriggerCSharp1.Run.");
            }
        }

        public static async Task<bool> DidPageChange(string pageUrl,
                                                    string lastChangedFileName)
        {
            bool dateChanged = false;
            try
            {
                LoggerFacade.LogInformation("Starting CheckIfPageChanged.");

                //LogEnvStrings();
                ConfigValues.Initialize();

                // Read in html for requested page.
                string pageText = GetPageText(pageUrl);
                // await SavePageText(pageText);

                // Parse out last changed date.
                if (pageText.Trim().Length > 0)
                {
                    string lastChangedDate = GetChangedDate(pageText);
                    
                    // Compare to date from the last time we checked the page.
                    // If different:
                    //      Save new date to check against last time.
                    //      Send message that page changed.
                    PageChangeTracker chgTracker = GetTracker(lastChangedFileName);
                    
                    if (chgTracker.HasDateChanged(lastChangedDate))
                    {
                        await chgTracker.SaveChangeDate(lastChangedDate);
                        var result = await SendMessage(lastChangedDate, pageUrl);
                        if (result)
                        {
                            dateChanged = true;
                        }
                    }
                    LoggerFacade.LogInformation($"Finished CheckIfPageChanged, dateChanged={dateChanged}.");
                }
                else
                {
                    LoggerFacade.LogError($"CheckIfPageChanged, unable to read from web page: {pageUrl}");
                }
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in TimerTriggerCSharp1.CheckIfPageChanged.");
                throw;
            }

            return dateChanged;
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
            // LoggerFacade.LogInformation($"response={htmlResponse}");
            LoggerFacade.LogInformation("Finished GetPageText.");

            return htmlResponse;
        }

        // Parse out the last changed date from the given text.
        // For the page that I am interested in, there is dateModified property
        // near the bottom of the page that appears to be updated whenever the page's
        // content is updated.
        public static string GetChangedDate(string htmlResponse)
        {
            // Searching from end because target is at bottom of page.
            string target = "dateModified";
            int startLoc = htmlResponse.LastIndexOf(target);
            LoggerFacade.LogInformation($"GetChangedDate, startLoc={startLoc}");
            string lastModified = "";
            if (startLoc > -1)
            {
                lastModified = htmlResponse.Substring(startLoc + target.Length + 2, 10);
                LoggerFacade.LogInformation($"GetChangedDate, dateModified={lastModified}");
            }
            else
            {
                LoggerFacade.LogInformation("GetChangedDate, about to throw exception.");
                throw new ArgumentException($"Unable to locate target text: {target} in provided text: {htmlResponse}");
            }
            return lastModified;
        }

        private static PageChangeTracker GetTracker(string fileName)
        {
            string shareName = ConfigValues.GetValue("shareName");
            string dirName = ConfigValues.GetValue("dirName");
            var azureFiles = new AzureFileShare(shareName, dirName);
            var chgTracker = new PageChangeTracker(fileName, azureFiles);
            
            return chgTracker;
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

        private static async Task SavePageText(string text)
        {
            await File.WriteAllTextAsync("samplePageText2.txt", text);
        }

    }
}
