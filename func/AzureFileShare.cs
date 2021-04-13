using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Logging;


namespace GAWUrlChecker
{
    // Provide access to given share and directory. Must call constructor before
    // any other methods.
    public class AzureFileShare
    {

        private static ShareClient share;
        private static ShareDirectoryClient directory;
        private static bool isInitialized = false;
        private static ILogger myLog;

        public AzureFileShare(string shareName, string dirName, ILogger log)
        {
            if (! isInitialized)
            {
                myLog = log;
                Task<bool> result = Initialize(shareName, dirName);
                if (result.Result)
                {
                    isInitialized = true;
                }
            }
        }

        private async Task<bool> Initialize(string shareName, string dirName)
        {
            bool isOk = false;

            string connectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            myLog.LogInformation($"connectionString={connectionString}");
            LogEnvStrings();

            share = new ShareClient(connectionString, shareName);
            await share.CreateIfNotExistsAsync();
            if (await share.ExistsAsync())
            {
                myLog.LogInformation("Finished share.ExistsAsync.");
                directory = share.GetDirectoryClient(dirName);
                await directory.CreateIfNotExistsAsync();
                if (await directory.ExistsAsync())
                {
                    myLog.LogInformation("Finished directory.ExistsAsync.");
                    isOk = true;
                }
            }
            return isOk;
        }

        public async Task<string> ReadFile(string fileName)
        {
            string fileContents = "";
            
            if (isInitialized)
                {
                myLog.LogInformation("Starting ReadFile");
                ReadKeyVaultValues();

                ShareFileClient file = directory.GetFileClient(fileName);
                myLog.LogInformation("Got file client.");
                Azure.Response<bool> fileExists = await file.ExistsAsync();
                //myLog.LogInformation($"file {fileName} exists={fileExists.ToString()}.");
                
                if (fileExists.Value)
                {
                    // Convert the string to a byte array, so can write to file.
                    using Stream stream = await file.OpenReadAsync();
                    {
                        myLog.LogInformation("Finished OpenReadAsync.");
                        byte[] result = new byte[stream.Length];
                        await stream.ReadAsync(result);
                        myLog.LogInformation("Finished ReadAsync.");
                        fileContents = System.Text.Encoding.UTF8.GetString(result);
                    }
                }
                else
                {
                    myLog.LogInformation( $"File {fileName} doesn't exist.");
                    fileContents = "";
                }
            }
            myLog.LogInformation($"Finished ReadFromFile, fileContents={fileContents}.");

            return fileContents;
        }

        public async Task<bool> WriteToFile(string fileName, 
                                                    string value)
        {
            bool wroteOk = false;
            
            myLog.LogInformation("Starting WriteToFile");

            if (isInitialized)
            {
                ShareFileClient file = directory.GetFileClient(fileName);
                myLog.LogInformation("Got file client.");
                // Convert the string to a byte array, so can write to file.
                byte[] bytes = new UTF8Encoding(true).GetBytes(value);
                myLog.LogInformation("Converted string to byte array.");
                var writeOptions = new ShareFileOpenWriteOptions();
                writeOptions.MaxSize = 200;
                using Stream stream = await file.OpenWriteAsync(overwrite: true,
                                                                position: 0, 
                                                                options: writeOptions);
                {
                    myLog.LogInformation("Finished OpenWriteAsync.");
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                    wroteOk = true;
                    myLog.LogInformation("Finished WriteAsync.");
                }
            }

            myLog.LogInformation("Finished WriteToFile.");

            return wroteOk;
        }

        public async Task<bool> DeleteFile(string fileName)
        {
            bool isDeleted = false;

            myLog.LogInformation("Starting DeleteFile");
            if (isInitialized)
            {
                ShareFileClient file = directory.GetFileClient(fileName);
                myLog.LogInformation("Got file client.");
                
                // Note: DeleteIfExistsAsync only returns true if file existed.
                var result = await file.DeleteIfExistsAsync();
                //Task<Response<bool>> result = await file.DeleteIfExistsAsync();
                isDeleted = true;
                if (result.Value == true)
                {
                    myLog.LogInformation("File existed, and was deleted.");
                }
               
            }

            return isDeleted;
        }

        private void ReadKeyVaultValues()
        {
            myLog.LogInformation($"Starting ReadKeyVaultValues");
            string vaultName = "urlcheckerkvus";
            string secretName = "secret1";
            string secretCfgName = "secretcfg";
            string vaultUri = $"https://{vaultName}.vault.azure.net/";
            myLog.LogInformation($"vaultUri={vaultUri}");

            string vaultKey = $"@Microsoft.KeyVault(VaultName={vaultName};SecretName={secretName}";
            string secretValue = System.Environment.GetEnvironmentVariable(vaultKey);
            myLog.LogInformation($"att1, secret value={secretValue}");

            vaultKey = $"@Microsoft.KeyVault(SecretUri={vaultUri}{secretName}/";
            secretValue = System.Environment.GetEnvironmentVariable(vaultKey);
            myLog.LogInformation($"att2, secret value={secretValue}");

            secretValue = System.Environment.GetEnvironmentVariable(secretName);
            myLog.LogInformation($"att3, secret value={secretValue}");

            secretValue = System.Environment.GetEnvironmentVariable(secretCfgName);
            myLog.LogInformation($"att4, secret value={secretValue}");


            //var client = Azure.Security.KeyVault.Secrets.SecretClient
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
            try
            {
                var client = new SecretClient(new Uri(vaultUri), 
                                            new DefaultAzureCredential(), secOptions);
                var result = client.GetSecret(secretName);
                secretValue = result.Value.Value;
                myLog.LogInformation($"att5, secret value={secretValue}");
            }
            catch (AggregateException e)
            {
                string errString = $"Caught {e.InnerExceptions.Count} " + 
                                $"exceptions: " + 
                                $"{string.Join(", ", e.InnerExceptions.Select(x => x.Message))}";
                myLog.LogError(errString);
            }
            catch (RequestFailedException e)
            {
                string errString = $"Caught code {e.ErrorCode} " + 
                                $"status: {e.Status} " + 
                                $"message: {e.Message}" +
                                $"stack: {e.StackTrace}";
                myLog.LogError(errString);
            }
        }

        private void LogEnvStrings()
        {
            var envStrings = System.Environment.GetEnvironmentVariables();
            var sortedEnv = new SortedList(envStrings);
            myLog.LogInformation("Environment variables");
            foreach (string s in sortedEnv.Keys)
                myLog.LogInformation( $"key: {s}, value:{envStrings[s]}");
            myLog.LogInformation("--------");

        }
    }
}