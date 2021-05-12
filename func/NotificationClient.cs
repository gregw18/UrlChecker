using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

[assembly: InternalsVisibleTo("tests")]
namespace GAWUrlChecker
{
    // Adaptor for the AWS SNS functionality. Provides ability to send SNS messages (emails)
    // for a given topic.
    public class NotificationClient
    {
        private AmazonSimpleNotificationServiceClient client;
        private CancellationToken cancelToken;

        public NotificationClient()
        {
            try
            {
                string awsAccessKeyId = ConfigValues.GetValue("awsAccessKeyId");
                string awsSecretAccessKey = ConfigValues.GetValue("awsSecretAccessKey");
                string awsRegionName = ConfigValues.GetValue("awsRegionName");
                RegionEndpoint awsRegion = RegionEndpoint.GetBySystemName( awsRegionName);
                client = new AmazonSimpleNotificationServiceClient(awsAccessKeyId,
                                                                    awsSecretAccessKey,
                                                                    awsRegion);
                LoggerFacade.LogInformation("NotificationClient ctor, finished sns client ctor.");
                var tokenSrc = new CancellationTokenSource();
                cancelToken = tokenSrc.Token;
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in NotificationClient ctor.");
            }
            LoggerFacade.LogInformation("Finished NotificationClient ctor.");
        }

        // Send message. If topic doesn't already exist, creates it.
        public async Task<bool> SendSnsMessage(string topic, string msgText)
        {
            bool sentMsg = false;
         
            // Get the ARN for the topic. If don't find it, create the topic.
            LoggerFacade.LogInformation("Starting SendSnsMessage.");
            string topicArn = await GetTopicArn(topic);
            if (topicArn.Length == 0)
            {
                LoggerFacade.LogInformation($"SendSnsMessage, creating topic {topic}.");
                topicArn = await CreateTopic(topic);
            }

            LoggerFacade.LogInformation("SendSnsMessage, calling PublishAsync.");
            var pubResp = await client.PublishAsync(topicArn, msgText);
            if (pubResp.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                sentMsg = true;
            }
            LoggerFacade.LogInformation($"Response to publishing was {pubResp.HttpStatusCode.ToString()}.");
            
            return sentMsg;
        }

        // Expects AWS topic arns of the form "arn:aws:sns:ca-central-1:666777888999:TestTopic"
        // Checks that everything after the last ":" matches the requested topic.
        internal async Task<string> GetTopicArn(string topic)
        {
            LoggerFacade.LogInformation("Starting GetTopicArn.");
            string arn = "";
            var resp = await client.ListTopicsAsync(cancelToken);
            
            LoggerFacade.LogInformation($"GetTopicArn finished await, response = {resp.HttpStatusCode}, have {resp.Topics.Count} topics.");
            while (true)
            {
                foreach(Topic t in resp.Topics)
                {
                    LoggerFacade.LogInformation($"topic: {t.TopicArn}");
                    string thisName = t.TopicArn.Substring(t.TopicArn.LastIndexOf(":") + 1);
                    if (thisName == topic)
                    {
                        arn = t.TopicArn;
                        break;
                    }
                }
                if (resp.NextToken is null || resp.NextToken.Length == 0)
                {
                    break;
                }
                else
                {
                    resp = await client.ListTopicsAsync(resp.NextToken, cancelToken);
                }
            }
            LoggerFacade.LogInformation( $"Finished GetTopicArn, arn={arn}");

            return arn;
        }

        // Assumes that have already verified that topic doesn't exist.
        private async Task<string> CreateTopic(string topic)
        {
            LoggerFacade.LogInformation("Starting CreateTopic.");

            string arn = "";
            var createResp = await client.CreateTopicAsync(topic, cancelToken);
            if (createResp.HttpStatusCode.ToString() == "OK")
            {
                arn = createResp.TopicArn;
                LoggerFacade.LogInformation($"Created topic {topic}, " + 
                                        $"arn={arn}.");
            }
            LoggerFacade.LogInformation( $"Finished CreateTopic, topic = {topic}, " + 
                                            $"arn={arn}.");

            return arn;
        }
    }
}