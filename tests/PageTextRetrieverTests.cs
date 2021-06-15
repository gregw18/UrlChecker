using System.Net;

using Xunit;

using GAWUrlChecker;

namespace tests
{
    public class PageTextRetrieverTests
    {
        const string badUrl = @"http://www.222ontario.ca/invalidpage";

        // Test that page text retriever throws an exception, with data populated,
        // when asked to retrieve an invalid url.
        [Fact]
        public async void RequestInvalidUrl_GetException()
        {
            var myTarget = new TargetTextData(badUrl, "target", 3, 5);
            var myRetriever = new PageTextRetriever();
            var ex = await Record.ExceptionAsync(() => 
                myRetriever.GetTargetText(myTarget));
            Assert.NotNull(ex);
            Assert.IsType<WebException>(ex);
            Assert.True(ex.Data.Contains("GetPageFullText"));
        }
    }
}
