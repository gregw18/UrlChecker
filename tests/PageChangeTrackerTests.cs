using System.Threading.Tasks;

using Xunit;

using GAWUrlChecker;

namespace tests
{
    // Test that page change tracker is saving and comparing
    // results correctly.
    public class PageChangeTrackerTests : IClassFixture<ConfigFixture>
    {
        ConfigFixture fixture;
        AzureFileShareClient azureFileShare;

        public PageChangeTrackerTests(ConfigFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async void StoreMatchedText_Matches(int index)
        {
            string fileName = "goodtest.txt";
            string savedDate = "Jan 23, 2019";
            string newDate = savedDate;
            await TestFileMatch(fileName, index, savedDate, newDate, false);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        public async void StoreMismatchedText_NoMatch(int index)
        {
            string fileName = "goodtest2.txt";
            string savedDate = "Jan 2, 2019";
            string newDate = "Jan 13, 2019";
            await TestFileMatch(fileName, index, savedDate, newDate, true);
        }

        [Fact]
        public async void CheckInvalidFile_NoMatch()
        {
            string fileName = "badtest1.txt";
            await SetAzureShare();
            PageChangeTracker pageTracker = await PageChangeTracker.CreateAsync(fileName, azureFileShare);
            Assert.True(pageTracker.HasTextChanged(0, "Jan 3, 2019"));

            await azureFileShare.DeleteFile(fileName);
        }

        private async Task TestFileMatch(string fileName, 
                                        int index,
                                        string savedText, 
                                        string newText,
                                        bool expectedResult)
        {
            await SetAzureShare();
            PageChangeTracker pageTracker = await PageChangeTracker.CreateAsync(fileName, azureFileShare);
            pageTracker.HasTextChanged(index, savedText);
            var wroteOk = await pageTracker.SaveChanges();

            if (wroteOk)
            {
                Assert.Equal(expectedResult, pageTracker.HasTextChanged(index, newText));
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
            azureFileShare = await AzureFileShareClient.CreateAsync(shareName, dirName);
        }

    }
}
