using Xunit;

using GAWUrlChecker;

namespace tests
{
    // Test that package change tracker is saving and comparing
    // results correctly.
    public class PageChangeTrackerTests : IClassFixture<ConfigFixture>
    {
        ConfigFixture fixture;
        AzureFileShare azureFileShare;

        public PageChangeTrackerTests(ConfigFixture fixture)
        {
            this.fixture = fixture;
            string shareName = ConfigValues.GetValue("shareName");
            string dirName = ConfigValues.GetValue("dirName");
            azureFileShare = new AzureFileShare(shareName, dirName);
        }

        [Fact]
        public async void StoreMatchedDate_Matches()
        {
            string fileName = "goodtest.txt";
            PageChangeTracker pageTracker = new PageChangeTracker(fileName, azureFileShare);
            string expectedDate = "Jan 23, 2019";
            var wroteOk = await pageTracker.SaveChangeDate(expectedDate);

            if (wroteOk)
            {
                LoggerFacade.LogInformation($"expectedDate={expectedDate}.");
                Assert.False(pageTracker.HasDateChanged(expectedDate));
            }
            else
            {
                Assert.False(1==0);
            }
            await azureFileShare.DeleteFile(fileName);
        }

        [Fact]
        public async void StoreMisatchedDate_NoMatch()
        {
            string fileName = "goodtest2.txt";
            PageChangeTracker pageTracker = new PageChangeTracker(fileName, azureFileShare);
            string savedDate = "Jan 2, 2019";
            string checkDate = "Jan 13, 2019";
            var wroteOk = await pageTracker.SaveChangeDate(savedDate);

            if (wroteOk)
            {
                Assert.True(pageTracker.HasDateChanged(checkDate));
            }
            else
            {
                Assert.False(1==0);
            }
            await azureFileShare.DeleteFile(fileName);
        }

        [Fact]
        public async void CheckInvalidFile_NoMatch()
        {
            string fileName = "badtest1.txt";
            PageChangeTracker pageTracker = new PageChangeTracker(fileName, azureFileShare);
            Assert.True(pageTracker.HasDateChanged("Jan 3, 2019"));

            await azureFileShare.DeleteFile(fileName);
        }
    }
}
