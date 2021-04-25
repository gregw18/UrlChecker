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

        public PageChangeTracker()
        {
            try
            {
                string shareName = ConfigValues.GetValue("shareName");
                string dirName = ConfigValues.GetValue("dirName");
                fileName = ConfigValues.GetValue("lastChangedFileName");

                azureFiles = new AzureFileShare(shareName, dirName);
                LoggerFacade.LogInformation($"Finished PageChangeTracker ctor");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in PageChangeTracker.ctor.");
            }

        }

        public void SaveChangeDate(string newChangeDate)
        {
            Task<bool> task = azureFiles.WriteToFile(fileName, newChangeDate);
            LoggerFacade.LogInformation($"Finished SaveChangeDate, result={task.Result}.");
        }


        // Compare given string to saved string.
        public bool HasDateChanged(string newDate)
        {
            bool hasChanged = false;
            if (newDate != GetLastChangeDate())
            {
                hasChanged = true;
            }

            return hasChanged;
        }

        private string GetLastChangeDate()
        {
            Task<string> lastMod = azureFiles.ReadFile(fileName);
            LoggerFacade.LogInformation($"GetLastChangeDate, finished read, contents = {lastMod.Result}");

            return lastMod.Result;
        }

    }
}
