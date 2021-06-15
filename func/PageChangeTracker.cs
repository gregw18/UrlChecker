using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace GAWUrlChecker
{
    // Track whether a given web page was changed, by storing and retrieving
    // an element that we know will change in an Azure file share. Can store 
    // multiple values, for multiple sites, in one file. E.g.
    //      PreviousValue0=May 31, 2021
    //      PreviousValue1=April 2, 2020
    // Indexes controlled by caller, but assumed to match indexes for TargetTextData.
    // Reads all values from file in on startup. Values in memory are updated as necessary
    // when processing, then SaveChanges is called at end to write them back
    // to the Azure file share.
    public class PageChangeTracker
    {
        private AzureFileShareClient azureFiles;
        private string fileName;
        private Dictionary<int, string> savedValues;
        private bool anyChanges = false;
        private const string linePrefix = "PreviousValue";

        public static Task<PageChangeTracker> CreateAsync(string fileName,
                                                                AzureFileShareClient myShare)
        {
            var newObj = new PageChangeTracker();
            return newObj.Initialize(fileName, myShare);
        }

        private async Task<PageChangeTracker> Initialize(string myFileName, AzureFileShareClient myShare)
        {
            try
            {
                fileName = myFileName;
                azureFiles = myShare;
                await ReadSavedValues();
                LoggerFacade.LogInformation($"Finished PageChangeTracker.Initialize");
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in PageChangeTracker.Initialize.");
            }

            return this;
        }

        // Read in the "previous" values from the azure file share. Store in dictionary,
        // key = index for group. Eg. for webSiteUrl0, key is 0.
        private async Task ReadSavedValues()
        {
            string savedText = await GetSavedText();
            LoggerFacade.LogInformation($"ReadSavedValues, savedText={savedText}.");
            string[] lines = savedText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            savedValues = new Dictionary<int, string>();
            foreach (string line in lines)
            {
                if (line.Length > 0)
                {
                    int pos = line.IndexOf('=');
                    if (pos > 0)
                    {
                        // Split each line at =, parse key off end of first part.
                        int key = Int32.Parse(line[linePrefix.Length..pos]);
                        string value = line[(pos + 1)..];
                        savedValues.Add(key, value);
                        LoggerFacade.LogInformation($"ReadSavedValues, added key: {key} and value: {value}.");
                    }
                }
            }
        }

        // Compare given string to saved string.
        // If saved string is empty, indicates that this is the first time
        // has run, so is considered a change.
        public bool HasTextChanged(int key, string newText)
        {
            bool hasChanged = false;
            if (savedValues.ContainsKey(key))
            {
                if (newText != savedValues[key])
                {
                    hasChanged = true;
                }
            }
            else
            {
                hasChanged = true;
            }
            LoggerFacade.LogInformation($"HasTextChanged, hasChanged={hasChanged}, anyChanges={anyChanges}.");

            // If the value changed, save the new text, set flag that we need to write data back out.
            if (hasChanged)
            {
                savedValues[key] = newText;
                anyChanges = true;
            }

            return hasChanged;
        }

        // If any data has changed, save it all in the file.
        public async Task<bool> SaveChanges()
        {
            bool completed;
            if (anyChanges)
            {
                var outputSb = new StringBuilder();
                foreach (KeyValuePair<int, string> kvp in savedValues)
                {
                    outputSb.Append(linePrefix + kvp.Key.ToString() + "=" + kvp.Value + "\r\n");
                }
                completed = await azureFiles.WriteToFile(fileName, outputSb.ToString());

            }
            else completed = true;
            // LoggerFacade.LogInformation($"Finished SaveChanges, result={completed}.");

            return completed;
        }

        private async Task<string> GetSavedText()
        {
            string lastMod = await azureFiles.ReadFile(fileName);
            // LoggerFacade.LogInformation($"GetSavedText, finished read, contents = {lastMod}");

            return lastMod;
        }

    }
}
