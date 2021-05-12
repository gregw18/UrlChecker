using System;
using System.Threading.Tasks;


namespace GAWUrlChecker
{
    // Track last time a given web page was changed, by storing and retrieving
    // that date in an Azure file share.
    public class PageChangeTracker
    {
        private AzureFileShareClient azureFiles;
        private string fileName;

        public PageChangeTracker(string fileName, AzureFileShareClient myShare)
        {
            try
            {
                this.fileName = fileName;
                azureFiles = myShare;
                LoggerFacade.LogInformation($"Finished PageChangeTracker ctor");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in PageChangeTracker.ctor.");
            }

        }

        // Save the provided date in the configured file.
        public async Task<bool> SaveNewText(string newChangeDate)
        {
            var completed = await azureFiles.WriteToFile(fileName, newChangeDate);
            // LoggerFacade.LogInformation($"Finished SaveNewText, result={completed}.");

            return completed;
        }


        // Compare given string to saved string.
        // If saved string is empty, indicates that this is the first time
        // has run, so is considered a change.
        public async Task<bool> HasTextChanged(string newDate)
        {
            bool hasChanged = false;
            if (newDate != await GetSavedText())
            {
                hasChanged = true;
            }

            return hasChanged;
        }

        private async Task<string> GetSavedText()
        {
            string lastMod = await azureFiles.ReadFile(fileName);
            // LoggerFacade.LogInformation($"GetSavedText, finished read, contents = {lastMod}");

            return lastMod;
        }

    }
}
