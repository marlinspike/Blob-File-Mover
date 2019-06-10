using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.File;
using Microsoft.Azure.Storage.DataMovement;
using BlobMover.Models;
using System.Threading.Tasks;

namespace BlobMover
{
    public static class QueueToBlob
    {
        [FunctionName("QueueToBlob")]
        public static async void Run([QueueTrigger("file-items", Connection = "Storage_Connection_String")]string myQueueItem,
            ILogger log, ExecutionContext context) {
            string fileName = myQueueItem;//myQueueItem.Substring(myQueueItem.IndexOf("/", 1) + 1);
            string fileShortName = (new CloudFile(new Uri(fileName))).Name;
            var connStr = Utils.Utility.GetConfigurationItem(context, "Storage_Connection_String");
            var shareName = Utils.Utility.GetConfigurationItem(context, "Share-Out");
            var out_blobs = Utils.Utility.GetConfigurationItem(context, "Blob-Out");

            string directoryName = null;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);

            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();
            CloudFileShare fileShare = fileClient.GetShareReference(shareName); //Get a reference to the passed fileShare
            //Get a reference to the passed Directory
            CloudFileDirectory shareDirectory;

            if (String.IsNullOrEmpty(directoryName))
                shareDirectory = fileShare.GetRootDirectoryReference();
            else
                shareDirectory = fileShare.GetRootDirectoryReference().GetDirectoryReference(directoryName);

            // Get a reference to the file we created previously.
            CloudFile file = shareDirectory.GetFileReference(fileShortName);

            // Ensure that the file exists.
            var bFileExists = file.Exists();
            if (file.Exists()) {
                // Copy the file to Blob Storage
                var success = await copyFileToBlobStorage(file, out_blobs,"", shareName, fileName, context, log);
                if (success)
                    log.LogInformation($"Copied File to Blob: {fileName}");
            }
        }

#   
        public async static Task<bool> copyFileToBlobStorage(CloudFile file, string container, string blobPath, string fileShare, string fileName, ExecutionContext context, ILogger log) {
            var destConnStr = Utils.Utility.GetConfigurationItem(context, "Dest_Storage_Connection_String");
            var connStr = Utils.Utility.GetConfigurationItem(context, "Storage_Connection_String");

            string fileShortName = System.IO.Path.GetFileName(file.Uri.LocalPath);


            //Refactored this to ensure that you can copy between storage accounts in different clouds and/or subscriptions
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);
            CloudStorageAccount destStorageAccount = CloudStorageAccount.Parse(destConnStr);

            CloudFileShare cloudFileShare = storageAccount.CreateCloudFileClient().GetShareReference(fileShare);
            CloudFile source = cloudFileShare.GetRootDirectoryReference().GetFileReference(fileShortName);


            CloudBlobContainer blobContainer = destStorageAccount.CreateCloudBlobClient().GetContainerReference(container);
            CloudBlob target = blobContainer.GetBlockBlobReference(fileShortName);

            try {
                await TransferManager.CopyAsync(source, target, true);
                await source.DeleteAsync(); //delete the file from the out-file Share
                int statusCode = await AzTable.TableLogger.writeToTable(fileShortName, "Queue", Utils.Utility.NextHop.Blob_Out, context);
                if (statusCode >= 200)
                    log.LogInformation("File Motion logged to Table");
                return true;
            }
            catch(TransferException te) {
                var code = te.ErrorCode;
                log.LogCritical($"Error copying to Blob: {te}");
                Console.WriteLine("Exception copying Queue -> Blob: ");
                return false;
            }

            
           
        }


    }
}
