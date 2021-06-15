using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;


namespace GAWUrlChecker
{
    // Asychronously retrieve contents of requested url, find the specified element
    // and return the defined following text.
    public class PageTextRetriever
    {
        // Retrieve everything from requested url, then parse out the requested section.
        public async Task<string> GetTargetText(TargetTextData targetData)
        {
            string targetText = "";
            LoggerFacade.LogInformation("Starting GetTargetText.");

            // Read in html for requested page.
            string pageText = await GetPageFullText(targetData.targetUrl);
            if (pageText.Trim().Length > 0)
            {
                // Parse out the requested section.
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
            string htmlResponse;
            try
            {
                var uri = new Uri (url);
                htmlResponse = await new WebClient().DownloadStringTaskAsync(uri);
            }
            catch (WebException ex)
            {
                // Rethrow with url added, so caller can let user know which url caused a problem.
                LoggerFacade.LogError(ex, $"Exception in PageTextRetriever.GetPageFullText, for url {url}");
                ex.Data.Add("GetPageFullText", url);
                throw;
            }
            LoggerFacade.LogInformation("Finished GetPageFullText.");

            return htmlResponse;
        }

        // Parse out the requested section from the given text.
        // Need to provide a target - some hardcoded text near the text that changes
        // every time the page is updated - the offset from the end of the target to
        // the text that changes, and the length of the text that changes.
        // For example, on the page that I am interested in, there is a dateModified property
        // near the bottom of the page that appears to be updated whenever the page's
        // content is updated. The value that changes starts two characters later, and is
        // ten characters long.
        private string GetTargetTextFromPage(string htmlResponse, TargetTextData targetData)
        {
            // Searching from end of string because my target happened to be at bottom of page.
            int startLoc = htmlResponse.LastIndexOf(targetData.targetLabel);
            LoggerFacade.LogInformation($"GetTargetTextFrompage, startLoc={startLoc}");
            string targetText;
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


    // Helper class to keep track of data for tracking one website.
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

        public bool AreValuesSame(TargetTextData obj2)
        {
            bool areSame = false;
            if (targetUrl == obj2.targetUrl && 
                targetLabel == obj2.targetLabel &&
                targetOffset == obj2.targetOffset &&
                targetLength == obj2.targetLength)
            {
                areSame = true;
            }
            
            return areSame;
        }
    }
}
