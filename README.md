## Blob File Mover
This set of Azure Functions moves data between Azure Files and Azure Blobs. Azure Blobs have a convenient binding for Functions, but Azure Files (for now), does not. 
To enable Azure Files -> Blobs movement, I use a Timer Trigger on an Azure Function to poll, which then creates a Message on a Queue (for which there is a Function binding). This in turn
moves the file to Azure Blob. All data movement is logged in an Azure Table.

The reason queues are used between Files -> Blobs is so that each async file copy has the full 10-minute run time of an Azure function to complete.

## To use locally
Here's what your local.settings.json file needs to look like:
```
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "YOUR-CONNECTION-STRING-HERE",
	"Storage_Connection_String":"YOUR-CONNECTION-STRING-HERE",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "LogTableName": "FileMover",
    "Share-In": "file-in",
    "Share-Out": "file-out",
    "Blob-In": "blob-in",
    "Blob-Out": "blob-out",
    "Queue-Name": "file-items"
  }
}
```

## Infrastructure Needed
- Two Azure File Shares: __share-in__ and __share-out__, for Incoming and Outgoing files respectively
- Two Blob Containers: __blob-in__ and __blob-out__, for  Incoming and Outgoing blob storage respectively
- One Queue:  __file-items__, used to track requests to move files from __share-out__ to __blob-out__
- One Azure Table: __LogTableName__, Created on the fly if it doesn't already exist

## Data Flows
The following data flows are implemented:
- blob-in --> file-in
- file-out --> queue --> blob-out