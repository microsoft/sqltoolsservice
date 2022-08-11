//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
    public class BlobService
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
            ConnectionInfo connInfo;
            ConnectionService.Instance.TryFindConnection(
                   optionsParams.OwnerUri,
                   out connInfo);
            var response = new CreateSasResponse();

            if (connInfo == null)
            {
                await requestContext.SendError(SR.ConnectionServiceListDbErrorNotConnected(optionsParams.OwnerUri));
                return;
            }
            if (connInfo.IsCloud)
            {
                await requestContext.SendError(SR.NotSupportedCloudCreateSas);
                return;
            }
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "AzureBlob"))
            {
                // Connection gets disconnected when backup is done
                ServerConnection serverConnection = new ServerConnection(sqlConn);
                Server sqlServer = new Server(serverConnection);

                SharedAccessSignatureCreator sharedAccessSignatureCreator = new SharedAccessSignatureCreator(sqlServer);
                string sharedAccessSignature = sharedAccessSignatureCreator.CreateSqlSASCredential(optionsParams.StorageAccountName, optionsParams.BlobContainerKey, optionsParams.BlobContainerUri, optionsParams.ExpirationDate);
                response.SharedAccessSignature = sharedAccessSignature;
                await requestContext.SendResult(response);
            }
        }
    }
}
