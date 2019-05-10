using System;
using System.IO;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BlobMover.Utils;

namespace BlobMover
{
    public static class Function1
    {
        [FunctionName("BlobToFileCopy")]
        public static async Task RunAsync([BlobTrigger("blob-in/{name}", Connection = "")]Stream myBlob, string name, ILogger log, ExecutionContext context) {
            var connStr = Utils.Utility.GetConfigurationItem(context, "Storage_Connection_String");
            var fileShareName = Utils.Utility.GetConfigurationItem(context, "Share-In");
            string directoryName = null;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);
            CloudFile cloudFile = null;


            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();
            CloudFileShare fileShare = fileClient.GetShareReference(fileShareName); //Get a reference to the passed fileShare
            //Get a reference to the passed Directory
            CloudFileDirectory shareDirectory;

            if (String.IsNullOrEmpty(directoryName))
                shareDirectory = fileShare.GetRootDirectoryReference();
            else
                shareDirectory = fileShare.GetRootDirectoryReference().GetDirectoryReference(directoryName);


            cloudFile = shareDirectory.GetFileReference(name);
            cloudFile.UploadFromStreamAsync(myBlob);

            int statusCode = await AzTable.TableLogger.writeToTable(name, "Blob Storage",  Utils.Utility.NextHop.File_In, context);

        }
    }
}
