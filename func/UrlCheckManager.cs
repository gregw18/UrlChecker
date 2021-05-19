using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace GAWUrlChecker
{
    public class UrlCheckManager
    {
        public async Task<bool> CheckUrls(string lastChangedFileName)
        {
            bool pageChanged = false;
            try
            {
                LoggerFacade.LogInformation("Starting CheckUrls.");

                Task<PageChangeTracker> trackerTask = GetPageTracker(lastChangedFileName);

                List<Task<String>> pageTasks = GetTargets();
                PageChangeTracker chgTracker = await trackerTask;
                string[] pageStrings = await Task.WhenAll(pageTasks);

                // Compare to text from the last time we checked the page.
                // If different:
                //      Save new text to check against next time.
                //      Send message that page changed.
                // Note that could send the message but not save the change,
                // which would result in a second "changed" message the next day,
                // even though there was no change. However, this is better than
                // not sending a message, for my use case.
                StringBuilder message = new StringBuilder();
                for (int i = 0; i < pageStrings.Length; i++)
                {
                    if (chgTracker.HasTextChanged(i, pageStrings[i]))
                    {
                        chgTracker.SetNewText(i, pageStrings[i]);
                        message.Append(GetMessage(pageStrings[i], ConfigValues.GetTarget(i).targetUrl));
                    }
                }
                bool sentOk = false;
                if (message.Length > 0)
                {
                    pageChanged = true;
                    Task<bool> sendTask = SendMessage(message.ToString());
                    Task<bool> saveTask = chgTracker.SaveChanges();
                    bool savedOk = await saveTask;
                    sentOk = await sendTask;
                }
                LoggerFacade.LogInformation($"Finished CheckUrls, sentOk={sentOk}.");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in UrlCheckManager.CheckUrls.");
                throw;
            }

            return pageChanged;
        }

        // Create and return the PageChangeTracker.
        private async Task<PageChangeTracker> GetPageTracker(string fileName)
        {
            string shareName = ConfigValues.GetValue("shareName");
            string dirName = ConfigValues.GetValue("dirName");
            var azureFiles = await AzureFileShareClient.CreateAsync(shareName, dirName);
            var chgTracker = await PageChangeTracker.CreateAsync(fileName, azureFiles);
            
            return chgTracker;
        }


        // Create and start task to read in and parse each page.
        private List<Task<String>> GetTargets()
        {
            int numPages = ConfigValues.GetNumberOfTargets();
            var pageTasks = new List<Task<String>>();
            PageTextRetriever myRetriever = new PageTextRetriever();
            for (int i = 0; i < numPages; i++)
            {
                TargetTextData target = ConfigValues.GetTarget(i);
                pageTasks.Add(myRetriever.GetTargetText(target));
            }

            return pageTasks;
        }

        // Generate message for this page, given that it changed.
        private string GetMessage(string newText, string url)
        {
            string msg = $"The web page {url} changed, " + 
                         $"new value is {newText}";

            return msg;
        }

        // Use AWS SNS to send an email to the configured topic.
        private async Task<bool> SendMessage(string message)
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
    }
}
