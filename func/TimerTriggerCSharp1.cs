using System;
//using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//using Azure;
//using Azure.Storage;
//using Azure.Storage.Files.Shares;
//using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;


namespace GAWUrlChecker
{
    public static class TimerTriggerCSharp1
    {

        static AmazonSimpleNotificationServiceClient client;
        static System.Threading.CancellationToken cancelToken;
        static string myTopicArn;

        [FunctionName("TimerTriggerCSharp1")]
        public static void Run([TimerTrigger("0 0 5 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            Console.WriteLine("In Run.");

            string shareName = "vaccinepagechecker";
            string dirName = "webpage";
            string fileName = "lastmodified.txt";

            var azureFiles = new AzureFileShare(shareName, dirName);
            Task<bool> task = azureFiles.WriteValueToFile(fileName, "Jan 29, 2021");
            Console.WriteLine($"Finished write, result={task.Result}.");

            Task<string> lastMod = azureFiles.ReadValueFromFile(fileName);
            Console.WriteLine($"Finished read, contents = {lastMod.Result}");
            //string myUrl = @"https://www.canada.ca/en/public-health/services/diseases/2019-novel-coronavirus-infection/prevention-risks/covid-19-vaccine-treatment/vaccine-rollout.html";
            //string lastDate = GetLastModifiedDate(myUrl);
            //Console.WriteLine($"lastModified={lastDate}.");

            // Task<int> task = CreateAndSend();
            //Console.WriteLine($"Finished Run, result={task.Result}.");
        }

        public static string GetLastModifiedDate(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream data = response.GetResponseStream();
            string htmlResponse = "";
            using (StreamReader sr = new StreamReader(data))
            {
                htmlResponse = sr.ReadToEnd();
            }
            //Console.WriteLine($"response={htmlResponse}");

            // Searching from end because target is at bottom of page.
            string target = "dateModified";
            int startLoc = htmlResponse.LastIndexOf(target);
            string lastModified = htmlResponse.Substring(startLoc + target.Length + 2, 10);
            Console.WriteLine($"dateModified={lastModified}");
            
            return lastModified;
        }
        
        private static async Task<int> CreateAndSend()
        {
            Console.WriteLine("Starting CreateAndSend.");
            client = new AmazonSimpleNotificationServiceClient();
            var tokenSrc = new CancellationTokenSource();
            cancelToken = tokenSrc.Token;

            string myTopic = "TestTopic";
            string msgText = "This is a test message.";
            Console.WriteLine("CreateAndSend about to await.");
            if (await CreateSnsTopic(myTopic))
            {
                bool sendResult = await SendSnsMessage(myTopic, msgText);
            }
            Console.WriteLine("Finished CreateAndSend.");
            
            return 0;
        }

        public async static Task<bool> CreateSnsTopic(string myTopic)
        {
            Console.WriteLine("Starting CreateSnsTopic.");
            bool found = false;
            var resp = await client.ListTopicsAsync(cancelToken);
            
            Console.WriteLine($"CreateSnsTopic finished await, response = {resp.HttpStatusCode}, have {resp.Topics.Count} topics.");
            foreach(Topic t in resp.Topics)
            {
                Console.WriteLine($"topic: {t.TopicArn}");
                if (t.TopicArn.EndsWith(myTopic))
                {
                    myTopicArn = t.TopicArn;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var createResp = await client.CreateTopicAsync(myTopic, cancelToken);
                if (createResp.HttpStatusCode.ToString() == "OK")
                {
                    myTopicArn = createResp.TopicArn;
                    Console.WriteLine($"Didn't find topic, so created new, arn={myTopicArn}.");
                    found = true;
                }
            }
            Console.WriteLine( $"Finished CreateSnsTopic, found={found}.");

            return found;
        }

        public static async Task<bool> SendSnsMessage(string myTopic, string msgText)
        {
            bool sentMsg = false;
            var pubResp = await client.PublishAsync(myTopicArn, msgText);
            if (pubResp.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                sentMsg = true;
            }
            Console.WriteLine($"Response to publishing was {pubResp.HttpStatusCode.ToString()}.");
            
            return sentMsg;
        }
    }
}
