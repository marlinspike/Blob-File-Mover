## Blob File Mover
This set of Azure Functions moves data between Azure Files and Azure Blobs. Azure Blobs have a convenient binding for Functions, but Azure Files (for now), does not. 
To enable Azure Files -> Blobs movement, I use a Timer Trigger on an Azure Function, which then moves the file to Azure Blob.
All data movement is logged in an Azure Table.

## To use locally
Here's what your local.settings.json file needs to look like:

{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "<Your Connection String Here>",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "Share-In": "file-in",
    "Share-Out": "file-out",
    "Blob-In": "blob-in",
    "Blob-Out": "blob-out",
    "Queue-Name": "file-items"
  }
}