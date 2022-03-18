//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.AzureBlob.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.AzureBlob
{
    class BlobService
    {
        private static readonly Lazy<BlobService> instance = new Lazy<BlobService>(() => new BlobService());

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal BlobService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static BlobService Instance
        {
            get { return instance.Value; }
        }

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            serviceHost.SetRequestHandler(CreateSasRequest.Type, HandleCreateSasRequest);
        }

        internal async Task HandleCreateSasRequest(
           CreateSasParams optionsParams,
           RequestContext<CreateSasResponse> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionService.Instance.TryFindConnection(
                       optionsParams.OwnerUri,
                       out connInfo);
                var response = new CreateSasResponse();

                if (connInfo != null && !connInfo.IsCloud)
                {
                    using (
                    SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "AzureBlob"))
                    {
                        // Connection gets discounnected when backup is done
                        ServerConnection serverConnection = new ServerConnection(sqlConn);
                        Server sqlServer = new Server(serverConnection);

                        SharedAccessSignatureCreator sharedAccessSignatureCreator = new SharedAccessSignatureCreator(sqlServer);
                        string sharedAccessSignature = sharedAccessSignatureCreator.CreateSqlSASCredential(optionsParams.StorageAccountName, optionsParams.BlobContainerKey, optionsParams.BlobContainerUri, optionsParams.ExpirationDate);
                        response.SharedAccessSignature = sharedAccessSignature;
                    }
                } else
                {
                    await requestContext.SendError("Create shared access signature is not supported opeation.");
                }
                await requestContext.SendResult(response);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }
    }
}
