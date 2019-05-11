using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.File;
using Microsoft.Azure.Storage.DataMovement;

namespace BlobMover
{
    public static class QueueToBlob
    {
        [FunctionName("QueueToBlob")]
        public async static void Run([QueueTrigger("file-items", Connection = "Storage_Connection_String")]string myQueueItem, ILogger log, ExecutionContext context) {
            string fileName = myQueueItem;//myQueueItem.Substring(myQueueItem.IndexOf("/", 1) + 1);
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
            CloudFile file = shareDirectory.GetFileReference((new CloudFile(new Uri(fileName)).Name));

            // Ensure that the file exists.
            var bFileExists = file.Exists();
            if (file.Exists()) {
                // Write the contents of the file to the console window.
                copyFileToBlobStorage(file, out_blobs,"", shareName, fileName, context);
            }
        }

        public async static void copyFileToBlobStorage(CloudFile file, string container, string blobPath, string fileShare, string fileName, ExecutionContext context) {
            var connStr = Utils.Utility.GetConfigurationItem(context, "Storage_Connection_String");
            string fileShortName = System.IO.Path.GetFileName(file.Uri.LocalPath);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);
            CloudFileShare cloudFileShare = storageAccount.CreateCloudFileClient().GetShareReference(fileShare);
            CloudFile source = cloudFileShare.GetRootDirectoryReference().GetFileReference(fileShortName);

            CloudBlobContainer blobContainer = storageAccount.CreateCloudBlobClient().GetContainerReference(container);
            CloudBlob target = blobContainer.GetBlockBlobReference(fileShortName);

            try {
                await TransferManager.CopyAsync(source, target, true);
                await source.DeleteAsync(); //delete the file from the out-file Share
            }
            catch(TransferException te) {
                Console.WriteLine("Exception copying Queue -> Blob: ");
            }

            
            int statusCode = await AzTable.TableLogger.writeToTable(fileShortName, "Queue", Utils.Utility.NextHop.Blob_Out, context);
        }


    }
}
