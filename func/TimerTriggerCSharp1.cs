using System;
using System.Collections;
using System.Collections.Generic;
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
        public static void Run([TimerTrigger("0 0 5 * * *")]TimerInfo myTimer, 
                                ILogger log)
        {
            LoggerFacade.UseILogger(log);
            LoggerFacade.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            LoggerFacade.LogInformation("In Run.");

            LogEnvStrings();
            //ReadKeyVaultValues();
            ConfigValues.Initialize();

            //string shareName = "vaccinepagechecker";
            //string dirName = "webpage";
            //string fileName = "lastmodified.txt";
            string shareName = ConfigValues.GetValue("shareName");
            string dirName = ConfigValues.GetValue("dirName");
            string fileName = ConfigValues.GetValue("lastChangedFileName");

            var azureFiles = new AzureFileShare(shareName, dirName);
            Task<bool> task = azureFiles.WriteToFile(fileName, "Jan 29, 2021");
            LoggerFacade.LogInformation($"Finished write, result={task.Result}.");

            Task<string> lastMod = azureFiles.ReadFile(fileName);
            LoggerFacade.LogInformation($"Finished read, contents = {lastMod.Result}");
            //string myUrl = @"https://www.canada.ca/en/public-health/services/diseases/2019-novel-coronavirus-infection/prevention-risks/covid-19-vaccine-treatment/vaccine-rollout.html";
            //string lastDate = GetLastModifiedDate(myUrl);
            //LoggerFacade.LogInformation($"lastModified={lastDate}.");

            // Task<int> task = CreateAndSend();
            //LoggerFacade.LogInformation($"Finished Run, result={task.Result}.");
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
            //LoggerFacade.LogInformation($"response={htmlResponse}");

            // Searching from end because target is at bottom of page.
            string target = "dateModified";
            int startLoc = htmlResponse.LastIndexOf(target);
            string lastModified = htmlResponse.Substring(startLoc + target.Length + 2, 10);
            LoggerFacade.LogInformation($"dateModified={lastModified}");
            
            return lastModified;
        }
        
        private static async Task<int> CreateAndSend()
        {
            LoggerFacade.LogInformation("Starting CreateAndSend.");
            client = new AmazonSimpleNotificationServiceClient();
            var tokenSrc = new CancellationTokenSource();
            cancelToken = tokenSrc.Token;

            string myTopic = "TestTopic";
            string msgText = "This is a test message.";
            LoggerFacade.LogInformation("CreateAndSend about to await.");
            if (await CreateSnsTopic(myTopic))
            {
                bool sendResult = await SendSnsMessage(myTopic, msgText);
            }
            LoggerFacade.LogInformation("Finished CreateAndSend.");
            
            return 0;
        }

        public async static Task<bool> CreateSnsTopic(string myTopic)
        {
            LoggerFacade.LogInformation("Starting CreateSnsTopic.");
            bool found = false;
            var resp = await client.ListTopicsAsync(cancelToken);
            
            LoggerFacade.LogInformation($"CreateSnsTopic finished await, response = {resp.HttpStatusCode}, have {resp.Topics.Count} topics.");
            foreach(Topic t in resp.Topics)
            {
                LoggerFacade.LogInformation($"topic: {t.TopicArn}");
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
                    LoggerFacade.LogInformation($"Didn't find topic, so created new, arn={myTopicArn}.");
                    found = true;
                }
            }
            LoggerFacade.LogInformation( $"Finished CreateSnsTopic, found={found}.");

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
            LoggerFacade.LogInformation($"Response to publishing was {pubResp.HttpStatusCode.ToString()}.");
            
            return sentMsg;
        }

/*
        private static void ReadKeyVaultValues()
        {
            LoggerFacade.LogInformation($"Starting ReadKeyVaultValues");
            string vaultName = "urlcheckerkvus";
            string secretName = "secret1";
            string secretCfgName = "secretcfg";
            string vaultUri = $"https://{vaultName}.vault.azure.net/";
            ConfigRetriever cfgRetriever = new ConfigRetriever(vaultUri);
            LoggerFacade.LogInformation($"vaultUri={vaultUri}");

            string vaultKey = $"@Microsoft.KeyVault(VaultName={vaultName};SecretName={secretName}";
            //string secretValue = System.Environment.GetEnvironmentVariable(vaultKey);
            string secretValue = cfgRetriever.ReadValue(vaultKey);
            LoggerFacade.LogInformation($"att1, secret value={secretValue}");

            vaultKey = $"@Microsoft.KeyVault(SecretUri={vaultUri}{secretName}/";
            //secretValue = System.Environment.GetEnvironmentVariable(vaultKey);
            secretValue = cfgRetriever.ReadValue(vaultKey);
            LoggerFacade.LogInformation($"att2, secret value={secretValue}");

            //secretValue = System.Environment.GetEnvironmentVariable(secretName);
            secretValue = cfgRetriever.ReadValue(vaultKey);
            LoggerFacade.LogInformation($"att3, secret value={secretValue}");

            secretValue = System.Environment.GetEnvironmentVariable(secretCfgName);
            LoggerFacade.LogInformation($"att4, secret value={secretValue}");

            secretValue = cfgRetriever.ReadSecret(secretName);
            LoggerFacade.LogInformation($"att5, secret value={secretValue}");
        }
*/
        public static void LogEnvStrings()
        {
            var envStrings = System.Environment.GetEnvironmentVariables();
            var sortedEnv = new SortedList(envStrings);
            LoggerFacade.LogInformation("\nEnvironment variables");
            foreach (string s in sortedEnv.Keys)
            {
                LoggerFacade.LogInformation( $"key: {s}, value:{envStrings[s]}");
            }
            LoggerFacade.LogInformation("--------\n");
        }

    }
}
