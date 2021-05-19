using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;


// Azure function, timer triggered, that checks whether a given web page has changed
// since the last time it was checked. If yes, sends an email to that effect.
// Uses "dateModified" property that happens to be in the web page that I'm interested in
// to tell whether the page has changed. Uses AWS SNS to send the email, as Azure doesn't
// appear to have service for sending emails, and I thought it would be interesting to see
// what it takes to get Azure and AWS to work together.
// Automatically publishes local settings to the azure function, for most configuration,
// but also stores some secrets in an Azure Key Vault - currently the AWS access keys.
// Stores date the page was last changed in an Azure Storage file share.

namespace GAWUrlChecker
{
    public static class UrlChecker
    {
        // Timer runs at 5am and 11am every day, eastern standard
        // (Cron expression uses UTC.)
        [FunctionName("UrlChecker")]
        public static async Task Run([TimerTrigger("0 0 10,16 * * *")]TimerInfo myTimer, 
                                ILogger log)
        {
            try
            {
                LoggerFacade.UseILogger(log);
                LoggerFacade.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                // LogEnvStrings();

                string previousContentsFileName = ConfigValues.GetValue("lastChangedFileName");
                var myChecker = new UrlCheckManager();
                bool result = await myChecker.CheckUrls(previousContentsFileName);
                LoggerFacade.LogInformation("Finished Run.");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in UrlChecker.Run.");
            }
        }

/*
        public static async Task<bool> CheckUrls(string lastChangedFileName)
        {
            bool pageChanged = false;
            try
            {
                LoggerFacade.LogInformation("Starting CheckUrls.");
                //LogEnvStrings();

                Task<PageChangeTracker> trackerTask = GetPageTracker(lastChangedFileName);

                // Create and start task to read in and parse each page.
                int numPages = ConfigValues.GetNumberOfTargets();
                var pageTasks = new List<Task<String>>();
                PageTextRetriever myRetriever = new PageTextRetriever();
                for (int i = 0; i < numPages; i++)
                {
                    TargetTextData target = ConfigValues.GetTarget(i);
                    pageTasks.Add(myRetriever.GetTargetText(target));
                }

                // Compare to text from the last time we checked the page.
                // If different:
                //      Save new text to check against next time.
                //      Send message that page changed.
                // Note that could send the message but not save the change,
                // which would result in a second "changed" message the next day,
                // even though there was no change. However, this is better than
                // not sending a message, for my use case.
                PageChangeTracker chgTracker = await trackerTask;
                string[] pageStrings = await Task.WhenAll(pageTasks);
                StringBuilder message = new StringBuilder();
                for (int i = 0; i < pageStrings.Length; i++)
                {
                    if (chgTracker.HasTextChanged(i, pageStrings[i]))
                    {
                        chgTracker.SetNewText(i, pageStrings[i]);
                        message.Append(GetMessage(pageStrings[i], ConfigValues.GetTarget(i).targetUrl));
                    }
                }
                Task<bool> sendTask = null;
                if (message.Length > 0)
                {
                    sendTask = SendMessage(message.ToString());
                }

                Task<bool> saveTask = chgTracker.SaveChanges();
                bool savedOk = await saveTask;
                bool sentOk = false;
                if (message.Length > 0)
                {
                    sentOk = await sendTask;
                }
                LoggerFacade.LogInformation($"Finished CheckUrls, sentOk={sentOk}.");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in UrlChecker.CheckUrls.");
                throw;
            }

            return pageChanged;
        }

        // Create and return the PageChangeTracker.
        private static async Task<PageChangeTracker> GetPageTracker(string fileName)
        {
            string shareName = ConfigValues.GetValue("shareName");
            string dirName = ConfigValues.GetValue("dirName");
            var azureFiles = await AzureFileShareClient.CreateAsync(shareName, dirName);
            var chgTracker = await PageChangeTracker.CreateAsync(fileName, azureFiles);
            
            return chgTracker;
        }

        // Use AWS SNS to send an email to the configured topic.
        private static string GetMessage(string newText, string url)
        {
            string msg = $"The web page {url} changed, " + 
                         $"new value is {newText}";

            return msg;
        }

        // Use AWS SNS to send an email to the configured topic.
        private static async Task<bool> SendMessage(string message)
        {
            bool sentOk = false;
            try
            {
                NotificationClient sns = new NotificationClient();
                string topic = ConfigValues.GetValue("snsTopic");
                LoggerFacade.LogInformation($"topic={topic}");
                sentOk = await sns.SendSnsMessage(topic, message);
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in SendMessage");
            }

            return sentOk;
        }
*/

        // Write all environment variables to the log.
        // Not a good idea if worried about others seeing these!
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
