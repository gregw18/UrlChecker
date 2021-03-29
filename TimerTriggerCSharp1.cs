using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace GAWTest1
{
    public static class TimerTriggerCSharp1
    {

        static AmazonSimpleNotificationServiceClient client;
        static System.Threading.CancellationToken cancelToken;


        [FunctionName("TimerTriggerCSharp1")]
        public static void Run([TimerTrigger("0 */2 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            Console.WriteLine("In Run.");
            //client = new AmazonSimpleNotificationServiceClient();
            //string myTopic = "TestTopic";
            //string msgText = "This is a test message.";
            //if (await CreateSnsTopic(myTopic))
            //    SendSnsMessage(myTopic, msgText);
            CreateAndSend();
            Console.WriteLine("Finished Run.");
        }

        //public static void main()
        //{
        //    Console.WriteLine("In main.");
        //    CreateAndSend();
        //}

        private static async void CreateAndSend()
        {
            Console.WriteLine("Starting CreateAndSend.");
            client = new AmazonSimpleNotificationServiceClient();
            string myTopic = "TestTopic";
            string msgText = "This is a test message.";
            Console.WriteLine("CreateAndSend about to await.");
            if (await CreateSnsTopic(myTopic))
                SendSnsMessage(myTopic, msgText);
            Console.WriteLine("Finished CreateAndSend.");
        }

        public async static Task<bool> CreateSnsTopic(string myTopic)
        {
            Console.WriteLine("Starting CreateSnsTopic.");
            bool found = false;
            var resp = await client.ListTopicsAsync(cancelToken);
            Console.WriteLine("CreateSnsTopic about to await.");
            foreach(Topic t in resp.Topics)
            {
                Console.WriteLine("topic: {t.TopicArn}");
                if (t.TopicArn == myTopic)
                {
                    found = true;
                    break;
                }
            }
            Console.WriteLine( "Finished CreateSnsTopic.");
            return found;
        }

        public static bool SendSnsMessage(string myTopic, string msgText)
        {
            Console.WriteLine("Didn't really send message: {msgText}");
            return false;
        }
    }
}
