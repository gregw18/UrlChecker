using System;

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Logging;

namespace GAWUrlChecker
{
    // Provide access to given share and directory. Must call constructor before
    // any other methods.
    public class ConfigRetriever
    {

        private ILogger myLog;

        private SecretClient client;
        private string lastVaultName = "ZZZ";

        public ConfigRetriever(ILogger log)
        {
            myLog = log;
        }

        // Read environment variable of the given name.
        public string ReadValue(string keyName)
        {
            string value = System.Environment.GetEnvironmentVariable(keyName) ?? "";
            myLog.LogInformation($"config {keyName}={value}");

            return value;
        }

        // Read given name in from secret in key vault.
        // If accessing a different vault, create a new SecretClient to 
        // access that vault.
        public string ReadSecret(string vaultName, string keyName)
        {
            string value = "";

            if (vaultName != lastVaultName)
            {
                SetSecretClient(vaultName);
                lastVaultName = vaultName;
            }
            try
            {
                var result = client.GetSecret(keyName);
                value = result.Value.Value;
                myLog.LogInformation($"secret {keyName}={value}");
            }
            catch (RequestFailedException e)
            {
                string errString = $"Caught code {e.ErrorCode} " + 
                                $"status: {e.Status} " + 
                                $"message: {e.Message}" +
                                $"stack: {e.StackTrace}";
                myLog.LogError(errString);
            }

            return value;
        }

        // Create a SecretClient to access the given vault.
        private void SetSecretClient(string vaultName)
        {
            SecretClientOptions secOptions = new SecretClientOptions()
            {
                Retry = 
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                }
            };

            string vaultUri = $"https://{vaultName}.vault.azure.net/";
            client = new SecretClient(new Uri(vaultUri), 
                                        new DefaultAzureCredential(), secOptions);
        }

    }
}
