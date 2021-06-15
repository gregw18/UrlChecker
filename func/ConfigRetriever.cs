using System;

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;


namespace GAWUrlChecker
{
    // Helper class for reading config values from environment and
    // secrets from an Azure Key Vault.
    public class ConfigRetriever
    {
        private SecretClient client;
        // If see a new vault name, have to create new client.
        private string lastVaultName = "ZZZ";

        // Read environment variable of the given name.
        public string ReadValue(string keyName)
        {
            string value = System.Environment.GetEnvironmentVariable(keyName) ?? "";

            return value;
        }

        // Read given name in from secret in key vault.
        // If accessing a different vault, change the SecretClient to 
        // access that vault.
        public string ReadSecret(string vaultName, string keyName)
        {
            string value = "";

            if (vaultName != lastVaultName || client is null)
            {
                SetSecretClient(vaultName);
                lastVaultName = vaultName;
            }

            try
            {
                var result = client.GetSecret(keyName);
                value = result.Value.Value;
            }
            catch (RequestFailedException e)
            {
                LoggerFacade.LogError(e, "Failed in ConfigRetriever.ReadSecret.");
            }

            return value;
        }

        // Create a SecretClient to access the given vault.
        private void SetSecretClient(string vaultName)
        {
            if (vaultName.Length > 0)
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
            else
            {
                LoggerFacade.LogError("ConfigRetriever.SetSecretClient called with empty vaultName.");
            }
        }

    }
}
