using Xunit;

using GAWUrlChecker;

namespace tests
{
    // Tests for AzureFileShareClient:
    //          Can read from existing file.
    //          Can overwrite existing file.
    //          Can write to new file.
    //          Get empty string when try to read from non-existing file.
    public class FileShareTests : IClassFixture<ConfigFixture>
    {
        private readonly string shareName;
        private readonly string dirName;

        public FileShareTests()
        {
            shareName = ConfigValues.GetValue("shareName");
            dirName = ConfigValues.GetValue("dirName");
        }

        [Fact]
        public async void ReadExistingFile_Succeeds()
        {
            string fileName = "ReadExisting.txt";
            string expectedText = "test data string";
            string actualContents = "";

            // Write data to the file.
            var azureFiles = await AzureFileShareClient.CreateAsync(shareName, dirName);
            var result1 = await azureFiles.WriteToFile(fileName, expectedText);
            if (result1)
            {
                LoggerFacade.LogInformation("ReadExisting test, finished first write.");

                // Read, now that we know that it exists.
                actualContents = await azureFiles.ReadFile(fileName);
                LoggerFacade.LogInformation("ReadExisting test, finished read.");
            }
            Assert.Equal(expectedText, actualContents);

            await azureFiles.DeleteFile(fileName);
        }

        [Fact]
        public async void WriteToExistingFile_Succeeds()
        {
            string fileName = "writeToExisting.txt";
            string expectedText = "data version 2";
            string actualText = "";
            LoggerFacade.LogInformation("Starting WriteToExisting test.");

            // Write once to file, to ensure exists.
            var azureFiles = await AzureFileShareClient.CreateAsync(shareName, dirName);
            var result1 = await azureFiles.WriteToFile(fileName, "data version 1");
            if (result1)
            {
                LoggerFacade.LogInformation("WriteToExisting test, finished first write.");

                // Write again, now that we know that it exists.
                var result2 = await azureFiles.WriteToFile(fileName, expectedText);
                if (result2)
                {
                    LoggerFacade.LogInformation("WriteToExisting test, finished second write.");
                    actualText = await azureFiles.ReadFile(fileName);
                    // Delete file.
                    await azureFiles.DeleteFile(fileName);
                    LoggerFacade.LogInformation("WriteToExisting test, finished delete.");
                }
            }
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public async void WriteToNewFile_Succeeds()
        {
            // Write once to file, to ensure exists.
            string actualText = "";
            string fileName = "writeToNew.txt";
            string expectedText = "data version 1";
            var azureFiles = await AzureFileShareClient.CreateAsync(shareName, dirName);
            bool result3 = await azureFiles.DeleteFile(fileName);
            if (result3)
            {
                var result1 = await azureFiles.WriteToFile(fileName, expectedText);
                if (result1)
                {
                    LoggerFacade.LogInformation("WriteToNew test, finished write.");
                    actualText = await azureFiles.ReadFile(fileName);

                    // Delete file.
                    await azureFiles.DeleteFile(fileName);
                    LoggerFacade.LogInformation("WriteToNew test, finished delete.");
                }
            }
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public async void ReadInvalidFile_ReturnsEmpty()
        {
            string fileName = "NotAFile.txt";
            string actualContents = "zzz";
            LoggerFacade.LogInformation("Starting ReadInvalid test.");

            // Ensure that file doesn't exist.
            var azureFiles = await AzureFileShareClient.CreateAsync(shareName, dirName);
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
