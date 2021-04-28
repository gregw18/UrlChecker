using System.Threading.Tasks;

using Xunit;

using GAWUrlChecker;

namespace tests
{
    // Test full process - set "last changed" value to a known value,
    // then run full check.
    // results correctly.
    public class SystemTests : IClassFixture<ConfigFixture>
    {
        ConfigFixture fixture;
        AzureFileShare azureFileShare;

        public SystemTests(ConfigFixture fixture)
        {
            this.fixture = fixture;
            string shareName = ConfigValues.GetValue("shareName");
            string dirName = ConfigValues.GetValue("dirName");
            azureFileShare = new AzureFileShare(shareName, dirName);
        }

        [Fact]
        public async void StoreMatchedDate_Matches()
        {
            // Read in current "last changed" date from web site, store it as
            // the saved date, then run the check - should match, unless
            // website changed in between the two calls.
            string fileName = "goodtest3.txt";
            string htmlText = TimerTriggerCSharp1.GetPageText(ConfigValues.GetValue("webSiteUrl"));
            string savedDate = TimerTriggerCSharp1.GetChangedDate(htmlText);
            var result = await azureFileShare.WriteToFile(fileName, savedDate);
            if (result)
            { 
                bool checkResult = await TimerTriggerCSharp1.DidPageChange(fileName);
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
            var result = await azureFileShare.WriteToFile(fileName, savedDate);
            if (result)
            { 
                bool checkResult = await TimerTriggerCSharp1.DidPageChange(fileName);
                Assert.True(checkResult);
            }
            else
            {
                Assert.True(false, $"Failed to save expected date to file: {fileName}");
            }
            await azureFileShare.DeleteFile(fileName);
        }
    }
}