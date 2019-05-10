using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace BlobMover.Models {
    class FileItemLog : TableEntity {

        public FileItemLog(string fileName, string source, string nextHop) {
            this.Source = source;
            this.NextHop = nextHop;
            this.id = Guid.NewGuid().ToString();
            this.LogTime = DateTime.Now;
            this.FileName = fileName;

            this.PartitionKey = id;
            this.RowKey = fileName;
        }

        public DateTime LogTime { get; set; }
        public string id { get; set; }
        public string FileName { get; set; }
        public string Source { get; set; }
        public string NextHop { get; set; }
        public string SourceType { get; set; }

    }
}
