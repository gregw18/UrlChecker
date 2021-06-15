using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace GAWUrlChecker
{
    // Manages entire process of checking each url and sending the message if
    // one or more have changed.
    public class UrlCheckManager
    {

        public bool sentErrors = false;

        public async Task<bool> HaveAnyPagesChanged(string lastChangedFileName)
        {
            bool pageChanged = false;
            try
            {
                LoggerFacade.LogInformation("Starting HaveAnyPagesChanged.");

                Task<PageChangeTracker> trackerTask = GetPageTracker(lastChangedFileName);
                PageChangeTracker chgTracker = await trackerTask;

                string messages = await GetChangeMessages(chgTracker);

                // If found differences:
                //      Send message that page changed.
                //      Save the new values.
                // Note that could send the message but not save the changes,
                // which would result in a second "changed" message the next day,
                // even though there was no change. However, this is better than
                // not sending a message, for my use case.
                bool sentOk = false, savedOk = false;
                if (messages.Length > 0)
                {
                    pageChanged = true;
                    Task<bool> sendTask = SendMessage(messages);
                    Task<bool> saveTask = chgTracker.SaveChanges();
                    savedOk = await saveTask;
                    sentOk = await sendTask;
                }
                LoggerFacade.LogInformation($"HaveAnyPagesChanged, sentOk={sentOk}, savedOk={savedOk}.");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in UrlCheckManager.HaveAnyPagesChanged.");
                throw;
            }
            LoggerFacade.LogInformation($"Finished HaveAnyPagesChanged, pageChanged={pageChanged}.");

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

        // For each page, retrieve the current text, compare to text from the last time we
        // checked the page. If different, create message that page changed.
        private async Task<string> GetChangeMessages(PageChangeTracker chgTracker)
        {
            StringBuilder message = new StringBuilder();
            try
            {
                // Get current values for each url.
                List<Task<String>> pageTasks = GetTargets();
                string[] pageStrings = await Task.WhenAll(pageTasks);

                // Check if value changed - append message if it did.
                for (int i = 0; i < pageStrings.Length; i++)
                {
                    if (chgTracker.HasTextChanged(i, pageStrings[i]))
                    {
                        message.Append(GetMessage(pageStrings[i], ConfigValues.GetTarget(i).targetUrl));
                    }
                }
            }
            catch (ArgumentException ex)
            {
                // If can't find target text on web page, send message that there is a problem.
                LoggerFacade.LogError(ex, "Exception in UrlCheckManager.GetChangeMessages.");
                string msg = ex.Message;
                sentErrors = await SendMessage(msg);
            }
            catch (WebException ex)
            {
                LoggerFacade.LogError(ex, "Exception in UrlCheckManager.GetChangeMessages.");

                // If get webException, and unable to retrieve web page, send message that
                // there was a problem.
                if (ex.Data.Contains("GetPageFullText"))
                {
                    string msg = "It seems that there was a problem retrieving the url:\n"
                        + ex.Data["GetPageFullText"] + "\n" + ex.Message;
                    sentErrors = await SendMessage(msg);
                }
            }
            LoggerFacade.LogInformation($"GetChangeMessages, message={message}");

            return message.ToString();
        }

        // Generate message for this page, given that it changed.
        private string GetMessage(string newText, string url)
        {
            string msg = $"The web page {url} changed, " + 
                         $"new value is {newText}{Environment.NewLine}\r\n\r\n";

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
