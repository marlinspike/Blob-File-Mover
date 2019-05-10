using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;

namespace BlobMover
{
    public static class FileToQueueMessage {
   

        [FunctionName("FileToQueueMessage")]
        public static async void RunAsync([TimerTrigger("*/45 * * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context) {
            //log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var connStr = Utils.Utility.GetConfigurationItem(context, "Storage_Connection_String");

            var queueName = Utils.Utility.GetConfigurationItem(context, "Queue-Name");
            var fileShareName = Utils.Utility.GetConfigurationItem(context, "Share-Out");


            string directoryName = null;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);

            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();
            CloudFileShare fileShare = fileClient.GetShareReference(fileShareName); //Get a reference to the passed fileShare
            //Get a reference to the passed Directory
            CloudFileDirectory shareDirectory;

            if (String.IsNullOrEmpty(directoryName))
                shareDirectory = fileShare.GetRootDirectoryReference();
            else
                shareDirectory = fileShare.GetRootDirectoryReference().GetDirectoryReference(directoryName);

            List<IListFileItem> lstFiles = new List<IListFileItem>();

            FileContinuationToken token = null;

            if (fileShare.ExistsAsync().Result) {
                do {
                    FileResultSegment resultSegment = await shareDirectory.ListFilesAndDirectoriesSegmentedAsync(token);
                    lstFiles.AddRange(resultSegment.Results);
                    token = resultSegment.ContinuationToken;
                } while (token != null);
            }

            foreach (var file in lstFiles) {
                await addFileToQueueAsync(queueName, file.Uri.ToString(), connStr);
                string filename = System.IO.Path.GetFileName(file.Uri.LocalPath);
                int statusCode = await AzTable.TableLogger.writeToTable(filename, "Azure Files", Utils.Utility.NextHop.Queue, context);
            }
        }

        public static async System.Threading.Tasks.Task addFileToQueueAsync(string qName, string fileName, string connectionString) {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient qClient = storageAccount.CreateCloudQueueClient();
            CloudQueue q = qClient.GetQueueReference(qName);
            await q.CreateIfNotExistsAsync();

            CloudQueueMessage msg = new CloudQueueMessage(fileName);
            await q.AddMessageAsync(msg);
        }
    }
}
