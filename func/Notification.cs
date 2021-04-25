using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace GAWUrlChecker
{
    // Adaptor for the AWS SNS functionality. Provides ability to send SNS messages for
    // a given topic.
    public class Notification
    {
        private AmazonSimpleNotificationServiceClient client;
        private CancellationToken cancelToken;


        public Notification()
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
                LoggerFacade.LogInformation("Notification ctor, finished sns client ctor.");
                var tokenSrc = new CancellationTokenSource();
                cancelToken = tokenSrc.Token;
            }
            catch (Exception ex)
            {
                LoggerFacade.LogError(ex, "Exception in Notification ctor.");
            }
            LoggerFacade.LogInformation("Finished Notification ctor.");

        }

        // Send message. If topic doesn't already exist, creates it.
        public async Task<bool> SendSnsMessage(string topic, string msgText)
        {
            bool sentMsg = false;
         
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


/*
        private async Task<int> CreateAndSend( string topic, string msgText)
        {
            LoggerFacade.LogInformation("Starting CreateAndSend.");
            //var tokenSrc = new CancellationTokenSource();
            //cancelToken = tokenSrc.Token;

            //string myTopic = "TestTopic";
            //string msgText = "This is a test message.";
            LoggerFacade.LogInformation("CreateAndSend about to await.");
            if (await CreateTopic(topic))
            {
                bool sendResult = await SendSnsMessage(topic, msgText);
            }
            LoggerFacade.LogInformation("Finished CreateAndSend.");
            
            return 0;
        }
*/

        private async Task<string> GetTopicArn(string topic)
        {
            LoggerFacade.LogInformation("Starting GetTopicArn.");
            string arn = "";
            var resp = await client.ListTopicsAsync(cancelToken);
            
            LoggerFacade.LogInformation($"GetTopicArn finished await, response = {resp.HttpStatusCode}, have {resp.Topics.Count} topics.");
            foreach(Topic t in resp.Topics)
            {
                LoggerFacade.LogInformation($"topic: {t.TopicArn}");
                if (t.TopicArn.EndsWith(topic))
                {
                    arn = t.TopicArn;
                    break;
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