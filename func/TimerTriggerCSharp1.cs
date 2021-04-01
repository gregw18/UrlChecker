using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
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
        static string myTopicArn;

        [FunctionName("TimerTriggerCSharp1")]
        public static void Run([TimerTrigger("0 0 5 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            Console.WriteLine("In Run.");

            string shareName = "vaccinepagechecker";
            string dirName = "webpage";
            string fileName = "lastmodified.txt";
            //Task<bool> task = WriteValueToFile(shareName, dirName, 
            //                                    fileName, "Jan 29, 2021");
            //Console.WriteLine($"Finished Run, result={task.Result}.");

            Task<string> lastMod = ReadValueFromFile(shareName, dirName, 
                                                fileName);
            Console.WriteLine($"Finished read, contents = {lastMod}");
            //string myUrl = @"https://www.canada.ca/en/public-health/services/diseases/2019-novel-coronavirus-infection/prevention-risks/covid-19-vaccine-treatment/vaccine-rollout.html";
            //string lastDate = GetLastModifiedDate(myUrl);
            //Console.WriteLine($"lastModified={lastDate}.");

            // Task<int> task = CreateAndSend();
            //Console.WriteLine($"Finished Run, result={task.Result}.");
        }

        public static async Task<string> ReadValueFromFile(string shareName, 
                                                    string dirName, 
                                                    string fileName)
        {
            string fileContents = "";
            
            Console.WriteLine("Starting ReadValueFromFile");

            string connectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            Console.WriteLine($"connectionString={connectionString}");

            ShareClient share = new ShareClient(connectionString, shareName);
            await share.CreateIfNotExistsAsync();
            if (await share.ExistsAsync())
            {
                Console.WriteLine("Finished share.ExistsAsync.");
                ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                await directory.CreateIfNotExistsAsync();
                if (await directory.ExistsAsync())
                {
                    Console.WriteLine("Finished directory.ExistsAsync.");
                    ShareFileClient file = directory.GetFileClient(fileName);
                    Console.WriteLine("Got file client.");
                    Azure.Response<bool> fileExists = await file.ExistsAsync();
                    //Console.WriteLine($"file {fileName} exists={fileExists.ToString()}.");
                    
                    if (fileExists)
                    {
                        // Convert the string to a byte array, so can write to file.
                        using Stream stream = await file.OpenReadAsync();
                        {
                            Console.WriteLine("Finished OpenReadAsync.");
                            byte[] result = new byte[stream.Length];
                            await stream.ReadAsync(result);
                            Console.WriteLine("Finished ReadAsync.");
                            fileContents = System.Text.Encoding.UTF8.GetString(result);
                        }
                    }
                    else
                    {
                        Console.WriteLine( $"File {fileName} doesn't exist.");
                        fileContents = "";
                    }

                }

            }
            Console.WriteLine($"Finished ReadValueFromFile, fileContents={fileContents}.");

            return fileContents;
        }

        public static async Task<bool> WriteValueToFile(string shareName, 
                                                    string dirName, 
                                                    string fileName,
                                                    string value)
        {
            bool wroteOk = false;
            
            Console.WriteLine("Starting WriteValueToFile");

            string connectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            Console.WriteLine($"connectionString={connectionString}");

            ShareClient share = new ShareClient(connectionString, shareName);
            await share.CreateIfNotExistsAsync();
            if (await share.ExistsAsync())
            {
                Console.WriteLine("Finished share.ExistsAsync.");
                ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                await directory.CreateIfNotExistsAsync();
                if (await directory.ExistsAsync())
                {
                    Console.WriteLine("Finished directory.ExistsAsync.");
                    ShareFileClient file = directory.GetFileClient(fileName);
                    Console.WriteLine("Got file client.");
                    //Azure.Response<bool> fileExists = await file.ExistsAsync();
                    //Console.WriteLine($"file {fileName} exists={fileExists.ToString()}.");
                    
                    //if (fileExists)
                    //{
                        // Convert the string to a byte array, so can write to file.
                        byte[] bytes = new UTF8Encoding(true).GetBytes(value);
                        Console.WriteLine("Converted string to byte array.");
                        var writeOptions = new ShareFileOpenWriteOptions();
                        writeOptions.MaxSize = 200;
                        using Stream stream = await file.OpenWriteAsync(overwrite: true,
                                                                        position: 0, 
                                                                        options: writeOptions);
                        {
                            Console.WriteLine("Finished OpenWriteAsync.");
                            await stream.WriteAsync(bytes, 0, bytes.Length);
                            wroteOk = true;
                            Console.WriteLine("Finished WriteAsync.");
                        }
                    //}

                }

            }
            Console.WriteLine("Finished WriteValueToFile.");

            return wroteOk;
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
            var pubResp = await client.PublishAsync(myTopicArn, msgText, cancelToken);
            if (pubResp.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                sentMsg = true;
            }
            Console.WriteLine($"Response to publishing was {pubResp.HttpStatusCode.ToString()}.");
            
            return sentMsg;
        }
    }
}
