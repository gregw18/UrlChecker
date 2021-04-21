using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using GAWUrlChecker;

namespace tests
{
    public class FileShareTests : IClassFixture<ConfigFixture>
    {
        ConfigFixture fixture;

        private string shareName;
        private string dirName;

        public FileShareTests(ConfigFixture fixture)
        {
            LoggerFacade.LogInformation("Starting FileShareTests ctor");
            this.fixture = fixture;
            shareName = ConfigValues.GetValue("shareName");
            dirName = ConfigValues.GetValue("dirName");

            LoggerFacade.LogInformation("Finished FileShareTests ctor");
        }


        [Fact]
        public async void WriteToExistingFile_Succeeds()
        {
            bool result3 = false;

            string fileName = "writeToExisting.txt";
            LoggerFacade.LogInformation("Starting WriteToExisting test.");

            // Write once to file, to ensure exists.
            var azureFiles = new AzureFileShare(shareName, dirName);
            var result1 = await azureFiles.WriteToFile(fileName, "data version 1");
            if (result1)
            {
                LoggerFacade.LogInformation("WriteToExisting test, finished first write.");

                // Write again, now that we know that it exists.
                var result2 = await azureFiles.WriteToFile(fileName, "data version 2");
                if (result2)
                {
                    LoggerFacade.LogInformation("WriteToExisting test, finished second write.");

                    // Delete file.
                    result3 = await azureFiles.DeleteFile(fileName);
                    LoggerFacade.LogInformation("WriteToExisting test, finished delete.");
                }
            }
            Assert.True(result3);
        }

    }
}
