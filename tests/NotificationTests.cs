using Xunit;

using GAWUrlChecker;

namespace tests
{
    // Tests for notification:
        // Get match for topic with complete name.
        // Don't get match for topic with invalid name
        // Don't get match for topic with partial match of end of name.
        // Don't get match for topic with partial match of beginning of name.
    public class NotificationTests : IClassFixture<ConfigFixture>
    {
        [Fact]
        public async void SendMsg_Succeeds()
        {
            var sns = new NotificationClient();
            string topic = ConfigValues.GetValue("snsTopic");
            LoggerFacade.LogInformation($"topic={topic}");
            string testMsg = "Message from SendMsg_Succeeds unit test.";
            bool result = await sns.SendSnsMessage(topic, testMsg);
            Assert.True(result);
        }

        [Fact]
        public async void ArnSearchMatch_Succeeds()
        {
            var sns = new NotificationClient();
            string topic = ConfigValues.GetValue("snsTopic");
            string fullArn = await sns.GetTopicArn(topic);
            
            Assert.True(fullArn.Length > 0);
        }

        [Fact]
        public async void ArnSearchTrailingPartialMatch_Fails()
        {
            var sns = new NotificationClient();
            // Skip first 10 characters of topic name and search for that - should fail.
            string topic = ConfigValues.GetValue("snsTopic")[10..];
            string fullArn = await sns.GetTopicArn(topic);
            
            Assert.Equal(0, fullArn.Length);
        }

        [Fact]
        public async void ArnSearchLeadingPartialMatch_Fails()
        {
            var sns = new NotificationClient();
            // Take first 20 characters of topic name and search for that - should fail.
            string topic = ConfigValues.GetValue("snsTopic").Substring(0, 20);
            string fullArn = await sns.GetTopicArn(topic);
            
            Assert.Equal(0, fullArn.Length);
        }

    }
}
