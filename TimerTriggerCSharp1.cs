using System;
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
        [FunctionName("TimerTriggerCSharp1")]
        public static void Run([TimerTrigger("0 18 6 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var client = new AmazonSimpleNotificationServiceClient();
            //client.PublishAsync()
        }
    }
}
