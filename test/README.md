# Testing

### Test Storage Account Setup

Some tests require access to an Azure Storage Account in order to test backup/restore functionality.  To run these tests, set these environment variables:

* `AzureStorageAccountName` : name of a storage account to execute tests with
* `AzureStorageAccountKey` : storage account key
* `AzureBlobContainerUri` : full URL of the blob container, e.g. `https://<testStorageAccount>.blob.core.windows.net/<testBlobContainer>`