using System;
using System.Threading.Tasks;

using Xunit;

using GAWUrlChecker;

namespace tests
{
    // Test full process - set "last changed" value to a known value,
    // then run full check.
    public class SystemTests : IClassFixture<ConfigFixture>
    {
        ConfigFixture fixture;
        AzureFileShareClient azureFileShare;

        public SystemTests(ConfigFixture fixture)
        {
            this.fixture = fixture;
            string shareName = ConfigValues.GetValue("shareName");
            string dirName = ConfigValues.GetValue("dirName");
        }

        [Fact]
        public async void StoreMatchedDate_Matches()
        {
            // Read in current "last changed" date from web site, store it as
            // the saved date, then run the check - should match, unless
            // website changed in between the two calls.
            string fileName = "goodtest3.txt";
            var targetData = ConfigValues.GetTarget(0);
            PageTextRetriever myRetriever = new PageTextRetriever();
            string savedDate = await myRetriever.GetTargetText(targetData);

            await SetAzureShare();
            var result = await azureFileShare.WriteToFile(fileName, savedDate);
            if (result)
            { 
                bool checkResult = await UrlChecker.CheckUrls(fileName);
                Assert.False(checkResult);
            }
            else
            {
                Assert.True(false, $"Failed to save expected date to file: {fileName}");
            }
            await azureFileShare.DeleteFile(fileName);
        }

        [Fact]
        public async void StoreMisMatchedDate_NoMatch()
        {
            // Store invalid value as the saved date, then run
            // the check - should not match.
            string fileName = "badtest2.txt";
            string savedDate = "Not a Valid Date";
            await SetAzureShare();
            var result = await azureFileShare.WriteToFile(fileName, savedDate);
            if (result)
            { 
                bool checkResult = await UrlChecker.CheckUrls(fileName);
                Assert.True(checkResult);
            }
            else
            {
                Assert.True(false, $"Failed to save expected date to file: {fileName}");
            }
            await azureFileShare.DeleteFile(fileName);
        }

        [Fact]
        public async void ReadInvalidPage_NoMatch()
        {
            // Try reading in a page that doesn't have the expected "modified last"
            // tag, expect to get an exception.
            string fileName = "badtest3.txt";
            string badUrl = "https://www.google.com/";
            string savedDate = "BadSaveDate";
            TargetTextData target = ConfigValues.GetTarget(0);
            string goodUrl = target.targetUrl;
            target.targetUrl = badUrl;
            await SetAzureShare();
            var result = await azureFileShare.WriteToFile(fileName, savedDate);
            if (result)
            { 
                Func<Task> act = () => UrlChecker.CheckUrls(fileName);
                await Assert.ThrowsAsync<ArgumentException>(act);
            }
            else
            {
                Assert.True(false, $"Failed to save expected date to file: {fileName}");
            }
            // Restore the good url for the next test.
            target.targetUrl = goodUrl;
            await azureFileShare.DeleteFile(fileName);
        }

        private async Task SetAzureShare()
        {
            string shareName = ConfigValues.GetValue("shareName");
            string dirName = ConfigValues.GetValue("dirName");
            azureFileShare = await AzureFileShareClient.CreateAsync(shareName, dirName);
        }

    }
}
