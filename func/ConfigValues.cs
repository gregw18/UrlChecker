using System;
using System.Collections.Generic;

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
        private static bool isInitialized = false;
        private static readonly object _locker = new object();
        private static Dictionary<string, string> config;

        public static bool Initialize()
        {
            lock (_locker)
            {
                if (!isInitialized)
                {
                    config = new Dictionary<string, string>();

                    // Seem to be stuck in a loop. Need key vault name to create the ConfigRetriever,
                    // but need the ConfigRetriever to retrieve the environment variable that
                    // contains the vault name.
                    // 1. Read vault name directly?
                    // 2. Modify ReadSecret to accept vault name, create client if doesn't already exist?

                    // Treating vaultName specially because it has to be available before
                    // can read a secret.
                    ConfigRetriever cfgRetriever = new ConfigRetriever();
                    string vaultName = "vaultName";
                    config.Add(vaultName, cfgRetriever.ReadValue(vaultName));

                    // Dictionary of config item names and whether each is a secret (i.e. is
                    // stored in the key vault, rather than an environment variable.)
                    Dictionary<string, bool> isSecret = new Dictionary<string, bool>();
                    isSecret.Add("secret1", true);
                    isSecret.Add("awsAccessKeyId", true);
                    isSecret.Add("awsSecretAccessKey", true);
                    isSecret.Add("webSiteUrl", false);
                    isSecret.Add("shareName", false);
                    isSecret.Add("dirName", false);
                    isSecret.Add("lastChangedFileName", false);
                    isSecret.Add("snsTopic", false);
                    isSecret.Add("awsRegionName", false);
                    isSecret.Add("targetText", false);
                    isSecret.Add("changingTextOffset", false);
                    isSecret.Add("changingTextLength", false);

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
                    isInitialized = true;
                    // LogValues();
                }
            }
            LoggerFacade.LogInformation("Finished ConfigValues.Initialize.");

            return isInitialized;
        }

        public static string GetValue(string key)
        {
            LoggerFacade.LogInformation($"GetValue, about to look for key: {key}");
            string value = "";
            try
            {
                value = config[key];
            }
            catch (ArgumentNullException ex)
            {
                LoggerFacade.LogError(ex, "ConfigValue.GetValue crashing because someone requested a null key");
            }
            catch (KeyNotFoundException ex)
            {
                string errMsg = $"ConfigValue.GetValue crashing because someone requested key: " + 
                         $"{key}, which doesn't exist.";
                LoggerFacade.LogError(ex, errMsg);
            }
            // LoggerFacade.LogInformation($"for key: {key}, found value: {value}");

            return value;
        }

        private static void LogValues()
        {
            foreach (var kvp in config)
            {
                LoggerFacade.LogInformation($"ConfigValues.LogValues, key={kvp.Key}, value={kvp.Value}");
            }
        }

    }
}

