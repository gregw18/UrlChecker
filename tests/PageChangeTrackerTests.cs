using System.Threading.Tasks;

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
        }

        [Fact]
        public async void StoreMatchedDate_Matches()
        {
            string fileName = "goodtest.txt";
            string savedDate = "Jan 23, 2019";
            string newDate = savedDate;
            await TestFileMatch(fileName, savedDate, newDate, false);
        }

        [Fact]
        public async void StoreMisatchedDate_NoMatch()
        {
            string fileName = "goodtest2.txt";
            //PageChangeTracker pageTracker = new PageChangeTracker(fileName, azureFileShare);
            string savedDate = "Jan 2, 2019";
            string newDate = "Jan 13, 2019";
            await TestFileMatch(fileName, savedDate, newDate, true);
        }

        [Fact]
        public async void CheckInvalidFile_NoMatch()
        {
            string fileName = "badtest1.txt";
            await SetAzureShare();
            PageChangeTracker pageTracker = new PageChangeTracker(fileName, azureFileShare);
            Assert.True(await pageTracker.HasDateChanged("Jan 3, 2019"));

            await azureFileShare.DeleteFile(fileName);
        }

        private async Task TestFileMatch(string fileName, 
                                        string savedDate, 
                                        string newDate,
                                        bool expectedResult)
        {
            await SetAzureShare();
            PageChangeTracker pageTracker = new PageChangeTracker(fileName, azureFileShare);
            var wroteOk = await pageTracker.SaveChangeDate(savedDate);

            if (wroteOk)
            {
                Assert.Equal(expectedResult, await pageTracker.HasDateChanged(newDate));
            }
            else
            {
                // If failed to write to the file, fail.
                Assert.False(1==0);
            }
            // Clean up file so don't have test files left on share.
            await azureFileShare.DeleteFile(fileName);
        }

        private async Task SetAzureShare()
        {
            string shareName = ConfigValues.GetValue("shareName");
            string dirName = ConfigValues.GetValue("dirName");
            azureFileShare = await AzureFileShare.CreateAsync(shareName, dirName);
        }

    }
}
