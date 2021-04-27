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
        public async void ReadExistingFile_Succeeds()
        {
            string fileName = "ReadExisting.txt";
            string fileContents = "test data string";
            string actualContents = "";
            LoggerFacade.LogInformation("Starting ReadExisting test.");

            // Write data to the file.
            var azureFiles = new AzureFileShare(shareName, dirName);
            var result1 = await azureFiles.WriteToFile(fileName, fileContents);
            if (result1)
            {
                LoggerFacade.LogInformation("ReadExisting test, finished first write.");

                // Read, now that we know that it exists.
                actualContents = await azureFiles.ReadFile(fileName);
                LoggerFacade.LogInformation("ReadExisting test, finished read.");
            }
            Assert.Equal(fileContents, actualContents);

            await azureFiles.DeleteFile(fileName);
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

        [Fact]
        public async void WriteToNewFile_Succeeds()
        {
            bool result3 = false;

            string fileName = "writeToNew.txt";
            LoggerFacade.LogInformation("Starting WriteToNew test.");

            // Write once to file, to ensure exists.
            var azureFiles = new AzureFileShare(shareName, dirName);
            result3 = await azureFiles.DeleteFile(fileName);
            if (result3)
            {
                var result1 = await azureFiles.WriteToFile(fileName, "data version 1");
                if (result1)
                {
                    LoggerFacade.LogInformation("WriteToNew test, finished write.");

                    // Delete file.
                    var result2 = await azureFiles.DeleteFile(fileName);
                    LoggerFacade.LogInformation("WriteToNew test, finished delete.");
                }
            }
            Assert.True(result3);
        }

        [Fact]
        public async void ReadInvalidFile_ReturnsEmpty()
        {
            string fileName = "NotAFile.txt";
            string actualContents = "zzz";
            LoggerFacade.LogInformation("Starting ReadInvalid test.");

            // Ensure that file doesn't exist.
            var azureFiles = new AzureFileShare(shareName, dirName);
            var result = await azureFiles.DeleteFile(fileName);
            if (result)
            {
                LoggerFacade.LogInformation("ReadExisting test, finished delete.");

                // Read, now that we know that it exists.
                actualContents = await azureFiles.ReadFile(fileName);
                LoggerFacade.LogInformation("ReadExisting test, finished read.");
            }
            Assert.True(actualContents.Length == 0);
        }

    }
}
