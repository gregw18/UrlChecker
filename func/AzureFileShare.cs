using System.IO;
using System.Text;
using System.Threading.Tasks;

using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;


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
            LoggerFacade.LogInformation($"connectionString={connectionString}");

            share = new ShareClient(connectionString, shareName);
            await share.CreateIfNotExistsAsync();
            if (await share.ExistsAsync())
            {
                LoggerFacade.LogInformation("Finished share.ExistsAsync.");
                directory = share.GetDirectoryClient(dirName);
                await directory.CreateIfNotExistsAsync();
                if (await directory.ExistsAsync())
                {
                    LoggerFacade.LogInformation("Finished directory.ExistsAsync.");
                    isOk = true;
                }
            }
            return isOk;
        }

        // Reads contents of requested file, from current file share/directory.
        public async Task<string> ReadFile(string fileName)
        {
            string fileContents = "";
            
            if (isInitialized)
                {
                LoggerFacade.LogInformation("Starting ReadFile");

                ShareFileClient file = directory.GetFileClient(fileName);
                LoggerFacade.LogInformation("Got file client.");
                Azure.Response<bool> fileExists = await file.ExistsAsync();
                //LoggerFacade.LogInformation($"file {fileName} exists={fileExists.ToString()}.");
                
                if (fileExists.Value)
                {
                    // Convert the string to a byte array, so can write to file.
                    using Stream stream = await file.OpenReadAsync();
                    {
                        LoggerFacade.LogInformation($"Finished OpenReadAsync, stream.Length = {stream.Length}.");
                        byte[] result = new byte[stream.Length];
                        await stream.ReadAsync(result);
                        LoggerFacade.LogInformation($"Finished ReadAsync, result.Length-{result.Length}.");
                        fileContents = System.Text.Encoding.UTF8.GetString(result, 0, result.Length);
                    }
                }
                else
                {
                    LoggerFacade.LogInformation( $"File {fileName} doesn't exist.");
                    fileContents = "";
                }
            }
            LoggerFacade.LogInformation($"Finished ReadFromFile, fileContents={fileContents}.");

            return fileContents;
        }

        // Write given text to requested file, in current file share/directory.
        public async Task<bool> WriteToFile(string fileName, 
                                                    string value)
        {
            bool wroteOk = false;
            
            LoggerFacade.LogInformation("Starting WriteToFile");

            if (isInitialized)
            {
                ShareFileClient file = directory.GetFileClient(fileName);
                LoggerFacade.LogInformation("Got file client.");
                // Convert the string to a byte array, so can write to file.
                byte[] bytes = new UTF8Encoding(true).GetBytes(value);
                LoggerFacade.LogInformation("Converted string to byte array.");
                var writeOptions = new ShareFileOpenWriteOptions();
                writeOptions.MaxSize = bytes.Length;
                using Stream stream = await file.OpenWriteAsync(overwrite: true,
                                                                position: 0, 
                                                                options: writeOptions);
                {
                    LoggerFacade.LogInformation("Finished OpenWriteAsync.");
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                    wroteOk = true;
                    LoggerFacade.LogInformation($"Finished WriteAsync, length={bytes.Length}.");
                }
            }
            LoggerFacade.LogInformation("Finished WriteToFile.");

            return wroteOk;
        }

        // Delete requested file, from current file share/directory.
        public async Task<bool> DeleteFile(string fileName)
        {
            bool isDeleted = false;

            LoggerFacade.LogInformation("Starting DeleteFile");
            if (isInitialized)
            {
                ShareFileClient file = directory.GetFileClient(fileName);
                LoggerFacade.LogInformation("Got file client.");
                
                if (file.Exists())
                {
                    // Note: DeleteIfExistsAsync only returns true if file existed.
                    var result = await file.DeleteIfExistsAsync();
                    if (result.Value == true)
                    {
                        isDeleted = true;
                        LoggerFacade.LogInformation("File existed, and was deleted.");
                    }
                }
                else
                {
                    // Am pretending that file was deleted, if it never existed.
                    LoggerFacade.LogInformation("File never existed.");
                    isDeleted = true;
                }
            }

            return isDeleted;
        }

    }
}