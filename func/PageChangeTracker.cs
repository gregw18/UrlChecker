using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;


namespace GAWUrlChecker
{
    // Track last time a given web page was changed, by storing and retrieving
    // that date in an Azure file share.
    public class PageChangeTracker
    {
        private AzureFileShare azureFiles;
        private string fileName;

        public PageChangeTracker(string fileName, AzureFileShare myShare)
        {
            try
            {
                //string shareName = ConfigValues.GetValue("shareName");
                //string dirName = ConfigValues.GetValue("dirName");
                this.fileName = fileName;

                //azureFiles = new AzureFileShare(shareName, dirName);
                azureFiles = myShare;
                LoggerFacade.LogInformation($"Finished PageChangeTracker ctor");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in PageChangeTracker.ctor.");
            }

        }

        public async Task<bool> SaveChangeDate(string newChangeDate)
        {
            var completed = await azureFiles.WriteToFile(fileName, newChangeDate);
            LoggerFacade.LogInformation($"Finished SaveChangeDate, result={completed}.");

            return completed;
        }


        // Compare given string to saved string.
        // If saved string is empty, indicates that this is the first time
        // has run, so is considered a change.
        public bool HasDateChanged(string newDate)
        {
            bool hasChanged = false;
            if (newDate != GetLastChangeDate().Result)
            {
                hasChanged = true;
            }

            return hasChanged;
        }

        private async Task<string> GetLastChangeDate()
        {
            string lastMod = await azureFiles.ReadFile(fileName);
            LoggerFacade.LogInformation($"GetLastChangeDate, finished read, contents = {lastMod}");

            return lastMod;
        }

    }
}
