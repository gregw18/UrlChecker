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

        public AzureFileShare(string shareName, string dirName)
        {
            if (! isInitialized)
            {
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
            Console.WriteLine($"connectionString={connectionString}");

            share = new ShareClient(connectionString, shareName);
            await share.CreateIfNotExistsAsync();
            if (await share.ExistsAsync())
            {
                Console.WriteLine("Finished share.ExistsAsync.");
                directory = share.GetDirectoryClient(dirName);
                await directory.CreateIfNotExistsAsync();
                if (await directory.ExistsAsync())
                {
                    Console.WriteLine("Finished directory.ExistsAsync.");
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
                Console.WriteLine("Starting ReadFile");

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
            Console.WriteLine($"Finished ReadFromFile, fileContents={fileContents}.");

            return fileContents;
        }

        public async Task<bool> WriteToFile(string fileName, 
                                                    string value)
        {
            bool wroteOk = false;
            
            Console.WriteLine("Starting WriteToFile");

            ShareFileClient file = directory.GetFileClient(fileName);
            Console.WriteLine("Got file client.");
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

            Console.WriteLine("Finished WriteToFile.");

            return wroteOk;
        }
    }
}