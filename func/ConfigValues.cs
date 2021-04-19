using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace GAWUrlChecker
{
    // Read in and provide access to config values for function.
    // Names of config values are hardcodes, as are whether they
    // are secrets - i.e. whether they are found in the key vault or in
    // environment variables.
    // Data stored in a dictionary of strings - if value isn't a string,
    // caller has to convert it.
    public static class ConfigValues
    {

        private static ILogger myLog;
        private static bool isInitialized = false;
        private static Dictionary<string, string> config;

        public static bool Initialize(ILogger log)
        {
            if (!isInitialized)
            {
                myLog = log;

                config = new Dictionary<string, string>();

                // Seem to be stuck in a loop. Need key vault name to create the ConfigRetriever,
                // but need the ConfigRetriever to retrieve the environment variable that
                // contains the vault name.
                // 1. Read vault name directly?
                // 2. Modify ReadSecret to accept vault name, create client if doesn't already exist?

                // Treating vaultName specially because it has to be available before
                // can read a secret.
                //string vaultName = "urlcheckerkvus";
                //string secretName = "secret1";
                //string secretCfgName = "secretcfg";
                ConfigRetriever cfgRetriever = new ConfigRetriever(myLog);
                string vaultName = "vaultName";
                config.Add(vaultName, cfgRetriever.ReadValue(vaultName));

                //myLog.LogInformation($"vaultUri={vaultUri}");

                //string vaultKey = $"@Microsoft.KeyVault(VaultName={vaultName};SecretName={secretName}";
                //string secretValue = System.Environment.GetEnvironmentVariable(vaultKey);
                //string secretValue = cfgRetriever.ReadValue(vaultKey);


                // Dictionary of config item names and whether each is a secret (i.e. is
                // stored in the key vault, rather than an environment variable.)
                Dictionary<string, bool> isSecret = new Dictionary<string, bool>();
                isSecret.Add("secret1", true);
                isSecret.Add("key1", false);

                // Read each item in and add name/value to the config dictionary.
                foreach (KeyValuePair<string, bool> kvp in isSecret)
                {
                    if (kvp.Value)
                    {
                        config.Add(kvp.Key, cfgRetriever.ReadSecret(config[vaultName], kvp.Key));
                    }
                    else
                    {
                        config.Add(kvp.Key, cfgRetriever.ReadValue(kvp.Key));
                    }
                }
                LogValues();
                isInitialized = true;
            }
            Console.WriteLine("Finished ConfigValues.Initialize.");

            return isInitialized;
        }

        public static string GetValue(string key)
        {
            Console.WriteLine($"GetValue, about to look for key: {key}");
            string value = "";
            try
            {
                value = config[key];
            }
            catch (ArgumentNullException ex)
            {
                myLog.LogError(ex, "ConfigValue.GetValue crashing because someone requested a null key");
            }
            catch (KeyNotFoundException ex)
            {
                string errMsg = $"ConfigValue.GetValue crashing because someone requested key: " + 
                         $"{key}, which doesn't exist.";
                myLog.LogError(ex, errMsg);
            }
            Console.WriteLine($"for key: {key}, found value: {value}");

            return value;
        }

        private static void LogValues()
        {
            foreach (var kvp in config)
            {
                Console.WriteLine($"ConfigValues.LogValues, key={kvp.Key}, value={kvp.Value}");
            }
        }

    }
}

