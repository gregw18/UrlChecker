using System;
using System.Collections.Generic;

namespace GAWUrlChecker
{
    // Read in and provide access to config values for function.
    // Names of config values are hardcoded, as are whether they
    // are secrets - i.e. whether they are found in the key vault or in
    // environment variables.
    // Data stored in a dictionary of strings - if value isn't a string,
    // caller has to convert it.
    public static class ConfigValues
    {
        private static bool isInitialized = false;
        private static readonly object _locker = new object();
        private static Dictionary<string, string> config;
        private static List<TargetTextData> targets;

        // Read in the requested variables from environment strings or key vault,
        // so they're ready to be accessed, and can be accessed the same regardless
        // of where they came from.
        public static bool Initialize()
        {
            lock (_locker)
            {
                if (!isInitialized)
                {
                    config = new Dictionary<string, string>();

                    // Treating vaultName specially because it has to be available before
                    // can read a secret.
                    ConfigRetriever cfgRetriever = new ConfigRetriever();
                    ReadGlobalValues(cfgRetriever);
                    ReadTargets(cfgRetriever);
                    /*
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
                    */
                    isInitialized = true;
                    // LogValues();
                }
            }
            LoggerFacade.LogInformation("Finished ConfigValues.Initialize.");

            return isInitialized;
        }

        // Looks up the requested key in the dictionary, returns corresponding value.
        // Returns empty string if key not found.
        public static string GetValue(string key)
        {
            LoggerFacade.LogInformation($"GetValue, about to look for key: {key}");
            if (!isInitialized)
            {
                Initialize();
            }

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

        public static int GetNumberOfTargets()
        {
            return targets.Count;
        }

        // If request a valid item, return it, otherwise return null;
        public static TargetTextData GetTarget(int index)
        {
            if (index > -1 && index < targets.Count)
                return targets[index];
            else return null;
        }

        // Read in config values other than targets.
        private static void ReadGlobalValues(ConfigRetriever cfgRetriever)
        {
            // Treating vaultName specially because it has to be available before
            // can read a secret.
            string vaultName = "vaultName";
            config.Add(vaultName, cfgRetriever.ReadValue(vaultName));

            // Dictionary of config item names and whether each is a secret (i.e. is
            // stored in the key vault, rather than an environment variable.)
            Dictionary<string, bool> isSecret = new Dictionary<string, bool>();
            isSecret.Add("secret1", true);
            isSecret.Add("awsAccessKeyId", true);
            isSecret.Add("awsSecretAccessKey", true);
            isSecret.Add("shareName", false);
            isSecret.Add("dirName", false);
            isSecret.Add("lastChangedFileName", false);
            isSecret.Add("snsTopic", false);
            isSecret.Add("awsRegionName", false);

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
        }

        // Read in values for all targets. Keep reading until find an entry
        // that is empty.
        private static void ReadTargets(ConfigRetriever cfgRetriever)
        {
            targets = new List<TargetTextData>();
            int i = 1;
            while (true)
            {
                string thisUrl = cfgRetriever.ReadValue("webSiteUrl" + i.ToString()).Trim();
                if (thisUrl.Length > 0)
                {
                    string thisLabel = cfgRetriever.ReadValue("targetText" + i.ToString()).Trim();
                    int thisOffset = Int32.Parse(cfgRetriever.ReadValue("targetTextOffset" + i.ToString()).Trim());
                    int thisLength = Int32.Parse(cfgRetriever.ReadValue("targetTextLength" + i.ToString()).Trim());
                    var thisTarget = new TargetTextData(thisUrl, thisLabel, thisOffset, thisLength);
                    targets.Add(thisTarget);
                }
                else break;
                i++;
            }
            LoggerFacade.LogInformation($"ReadTargets found {i-1} targets.");
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

