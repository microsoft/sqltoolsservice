//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using Microsoft.SqlServer.Management.Smo;
using Azure.Storage.Blobs;
using Azure.Storage;
using Azure.Storage.Sas;
using Microsoft.SqlServer.Management.Common;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.AzureBlob.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.AzureBlob
{
    class SharedAccessSignatureCreator
    {


        private Server sqlServer;

        public SharedAccessSignatureCreator(Server sqlServer)
        {
            this.sqlServer = sqlServer;
        }

        public string CreateSqlSASCredential(string accountName, string accountKey, string containerUri)
        {
            var containerClient = new BlobContainerClient(new Uri(containerUri), new StorageSharedKeyCredential(accountName, accountKey));
            Uri secretStringUri = GetServiceSasUriForContainer(containerClient);
            string secretString = secretStringUri.ToString().Split('?')[1];
            string identity = "Shared Access Signature";
            WriteSASCredentialToSqlServer(containerUri, identity, secretString);
            return secretString;
        }

        /// <summary>
        /// Create sql sas credential with the given credential name
        /// </summary>
        /// <param name="credentialName">Name of sas credential, here is the same of the full container url.</param>
        /// <param name="identity">Identity for credential, here is fixed as "Shared Access Signature"</param>
        /// <param name="secretString">Secret of credential, which is sharedAccessSignatureForContainer </param>
        /// <returns> The newly created SAS credential</returns>
        public Credential WriteSASCredentialToSqlServer(string credentialName, string identity, string secretString)
        {
            // Format of Sql SAS credential: 
            // CREATE CREDENTIAL [https://<StorageAccountName>.blob.core.windows.net/<ContainerName>] WITH IDENTITY = N'Shared Access Signature', 
            // SECRET = N'sv=2014-02-14&sr=c&sig=lxb2aXr%2Bi0Aeygg%2B0a4REZ%2BqsUxxxxxxsqUybg0tVzg%3D&st=2015-10-15T08%3A00%3A00Z&se=2015-11-15T08%3A00%3A00Z&sp=rwdl'
            //
            CredentialCollection credentials = sqlServer.Credentials;

            Credential azureCredential = new Credential(sqlServer, credentialName);

            // Container can have many SAS credentials coexisting, here we'll always drop existing one once customer choose to create new credential 
            // since sql customer has no way to know its existency and even harder to retrive its secret string. 
            if (credentials.Contains(credentialName))
            {
                Credential oldCredential = credentials[credentialName];
                oldCredential.Drop();
            }

            try
            {
                azureCredential.Create(identity, secretString);
            }
            catch (Exception ex)
            {
                throw new FailedOperationException("Create credential failed.", ex);
            }
            return azureCredential;
        }

        /// <summary>
        /// Create Shared Access Policy for container 
        /// Default Accesss permission is Write/List/Read/Delete
        /// </summary>
        /// <param name="container"></param>
        /// <param name="policyName"></param>
        /// <param name="selectedSaredAccessExpiryTime"></param>
        public Uri GetServiceSasUriForContainer(BlobContainerClient containerClient,
                                          string storedPolicyName = null,
                                          DateTimeOffset? expiringDate = null)
        {
            // Check whether this BlobContainerClient object has been authorized with Shared Key.
            if (containerClient.CanGenerateSasUri)
            {
                // Create a SAS token that's valid for one year.
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerClient.Name,
                    Resource = BlobSasResource.BLOB_CONTAINER
                };

                if (storedPolicyName == null)
                {
                    sasBuilder.ExpiresOn = (DateTimeOffset)(expiringDate == null ? DateTimeOffset.UtcNow.AddYears(1) : expiringDate);
                    sasBuilder.SetPermissions(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List | BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Delete);
                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }
                Uri sasUri = containerClient.GenerateSasUri(sasBuilder);

                return sasUri;
            }
            else
            {
                throw new FailedOperationException("Cannot generate SAS URI for blob container");            }
        }
    }
}
