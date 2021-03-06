using System.Threading.Tasks;

using Xunit;

using GAWUrlChecker;

namespace tests
{
    // Test full process - set "last changed" value to a known value,
    // then run full check.
    public class SystemTests : IClassFixture<ConfigFixture>
    {
        AzureFileShareClient azureFileShare;

        [Fact]
        public async void StoreMatchedDate_Matches()
        {
            // Read in current "last changed" date from web site, store it as
            // the saved date, then run the check - should match, unless
            // website changed in between the two calls.
            string fileName = "goodtest3.txt";
            var targetData = ConfigValues.GetTarget(0);
            var myRetriever = new PageTextRetriever();
            string savedDate = await myRetriever.GetTargetText(targetData);

            await SetAzureShare();
            var chgTracker = await PageChangeTracker.CreateAsync(fileName, azureFileShare);
            chgTracker.HasTextChanged(0, savedDate);
            var result = await chgTracker.SaveChanges();
            if (result)
            { 
                var myManager = new UrlCheckManager();
                bool checkResult = await myManager.HaveAnyPagesChanged(fileName);
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
            var chgTracker = await PageChangeTracker.CreateAsync(fileName, azureFileShare);
            chgTracker.HasTextChanged(0, savedDate);
            var result = await chgTracker.SaveChanges();
            if (result)
            { 
                var myManager = new UrlCheckManager();
                bool checkResult = await myManager.HaveAnyPagesChanged(fileName);
                Assert.True(checkResult);
            }
            else
            {
                Assert.True(false, $"Failed to save expected data to file: {fileName}");
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
                var myManager = new UrlCheckManager();
                await myManager.HaveAnyPagesChanged(fileName);
                Assert.True(myManager.sentErrors);
            }
            else
            {
                Assert.True(false, $"Failed to save expected date to file: {fileName}");
            }
            // Restore the good url for the next test.
            target.targetUrl = goodUrl;
            await azureFileShare.DeleteFile(fileName);
        }

        [Fact]
        public async void ReadGoodPageInvalidTarget_NoMatch()
        {
            // Read in a good page, but look for an invalid target
            // tag, expect to get an exception.
            string fileName = "badtest3.txt";
            string badTarget = "BAD_BAD_SOBAD";
            TargetTextData target = ConfigValues.GetTarget(0);
            string goodTarget = target.targetLabel;
            target.targetLabel = badTarget;
            await SetAzureShare();

            var myManager = new UrlCheckManager();
            await myManager.HaveAnyPagesChanged(fileName);
            Assert.True(myManager.sentErrors);

            // Restore the good url for the next test.
            target.targetLabel = goodTarget;
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
