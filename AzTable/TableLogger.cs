using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BlobMover.Models;
using BlobMover.Utils;
using Microsoft.Azure.WebJobs;

namespace BlobMover.AzTable {
    class TableLogger {
        public static string TABLE_NAME = "FileMover";
        public TableLogger() {

        }


        public async static Task<int> writeToTable(string fileName, string source, Utility.NextHop nextHop, ExecutionContext context) {
            var connStr = Utils.Utility.GetConfigurationItem(context, "AzureWebJobsStorage");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(TableLogger.TABLE_NAME);
            bool tableCreated = await table.CreateIfNotExistsAsync();

            FileItemLog fil = new FileItemLog(fileName, source, nextHop.ToString());



            TableOperation insertOp = TableOperation.InsertOrReplace(fil);
            TableResult result = await table.ExecuteAsync(insertOp);

            return result.HttpStatusCode;
        }

    }
}
