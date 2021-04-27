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
        public async void SendMsg_Succeeds()
        {
            Notification sns = new Notification();
            string topic = ConfigValues.GetValue("snsTopic");
            LoggerFacade.LogInformation($"topic={topic}");
            string testMsg = "test msg";
            bool result = await sns.SendSnsMessage(topic, testMsg);
            Assert.True(result);
        }

        // Get match for topic with complete name.
        // Don't get match for topic with invalid name
        // Don't get match for topic with partial match of end of name.
        // Don't get match for topic with partial match of beginning of name.

        [Fact]
        public async void ArnSearchMatch_Succeeds()
        {
            Notification sns = new Notification();
            string topic = ConfigValues.GetValue("snsTopic");
            string fullArn = await sns.GetTopicArn(topic);
            
            Assert.True(fullArn.Length > 0);
        }

        [Fact]
        public async void ArnSearchTrailingPartialMatch_Fails()
        {
            Notification sns = new Notification();
            string topic = ConfigValues.GetValue("snsTopic").Substring(10);
            string fullArn = await sns.GetTopicArn(topic);
            
            Assert.Equal(0, fullArn.Length);
        }

        [Fact]
        public async void ArnSearchLeadingPartialMatch_Fails()
        {
            Notification sns = new Notification();
            string topic = ConfigValues.GetValue("snsTopic").Substring(0, 20);
            string fullArn = await sns.GetTopicArn(topic);
            
            Assert.Equal(0, fullArn.Length);
        }

    }
}
