using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;



namespace GAWUrlChecker
{
    public class PageTextRetriever
    {
        // Timer runs at 5am and 11am every day, eastern standard
        // (Cron expression uses UTC.)


        public async Task<string> GetTargetText(TargetTextData targetData)
        {
            string targetText = "";
            LoggerFacade.LogInformation("Starting GetTargetText.");

            // Read in html for requested page.
            string pageText = await GetPageFullText(targetData.targetUrl);
            if (pageText.Trim().Length > 0)
            {
                // Parse out last changed date.
                targetText = GetTargetTextFromPage(pageText, targetData);
            }
            else
            {
                LoggerFacade.LogError($"GetTargetText, unable to read from web page: {targetData.targetUrl}");
            }

            return targetText;
        }

        // Return html from requested page.
        private async Task<string> GetPageFullText(string url)
        {
            LoggerFacade.LogInformation("Starting GetPageFullText.");
            var uri = new Uri (url);
            string htmlResponse = await new WebClient().DownloadStringTaskAsync(uri);
            LoggerFacade.LogInformation("Finished GetPageFullText.");

            return htmlResponse;
        }

        // Parse out the last changed date from the given text.
        // For the page that I am interested in, there is a dateModified property
        // near the bottom of the page that appears to be updated whenever the page's
        // content is updated.
        private string GetTargetTextFromPage(string htmlResponse, TargetTextData targetData)
        {
            // Searching from end of string because target is at bottom of page.
            int startLoc = htmlResponse.LastIndexOf(targetData.targetLabel);
            LoggerFacade.LogInformation($"GetTargetTextFrompage, startLoc={startLoc}");
            string targetText = "";
            if (startLoc > -1)
            {
                int targetStartLoc = startLoc + targetData.targetLabel.Length + targetData.targetOffset;
                targetText = htmlResponse.Substring(targetStartLoc, targetData.targetLength);
                LoggerFacade.LogInformation($"GetTargetTextFromPage, targetText={targetText}");
            }
            else
            {
                LoggerFacade.LogInformation("GetTargetTextFromPage, about to throw exception.");
                throw new ArgumentException($"Unable to locate target text: '{targetData.targetLabel}' " + 
                                    $"in url {targetData.targetUrl}, in retrieved text: {htmlResponse}");
            }
            return targetText;
        }

        // Helper to write full text from target page to a text file, for debugging.
        private async Task SavePageText(string text)
        {
            await File.WriteAllTextAsync("samplePageText2.txt", text);
        }

    }


    public class TargetTextData
    {
        public string targetUrl;        // Url to read.
        public string targetLabel;      // "Marker" text that doesn't change, to locate target text.
        public int targetOffset;        // Offset from end of label to start of target text.
        public int targetLength;        // Length of target text.

        public TargetTextData(string url, string label, int offset, int length)
        {
            targetUrl = url;
            targetLabel = label;
            targetOffset = offset;
            targetLength = length;
        }
    }
}
