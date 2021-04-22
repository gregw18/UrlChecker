using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using GAWUrlChecker;

namespace tests
{
    public class UnitTest1 : IClassFixture<ConfigFixture>
    {

        ConfigFixture fixture;

        public UnitTest1(ConfigFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void Test1()
        {
            int result = 4;
            Assert.Equal(4, result);
        }

        [Fact]
        public async void Test2()
        {
            Notification sns = new Notification();
            string topic = ConfigValues.GetValue("snsTopic");
            LoggerFacade.LogInformation($"topic={topic}");
            string testMsg = "test msg";
            bool result = await sns.SendSnsMessage(topic, testMsg);
            Assert.True(result);
        }

    }
}
