//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Parameters passed for creating shared access signature
    /// </summary>
    public class CreateSasParams
    {
        /// <summary>
        /// Sql server URI
        /// </summary>
        public string OwnerUri { get; set; }
        /// <summary>
        /// Blob container URI
        /// </summary>
        public string BlobContainerUri { get; set; }
        /// <summary>
        /// Blob container key
        /// </summary>
        public string BlobContainerKey { get; set; }
        /// <summary>
        /// Storage account name
        /// </summary>
        public string StorageAccountName { get; set; }
    }

    /// <summary>
    /// Response class for backup execution
    /// </summary>
    public class CreateSasResponse
    {
        public string SharedAccessSignature { get; set; }

    }

    /// <summary>
    /// Request class for backup execution
    /// </summary>
    public class CreateSasRequest
    {
        public static readonly
            RequestType<CreateSasParams, CreateSasResponse> Type =
                RequestType<CreateSasParams, CreateSasResponse>.Create("blob/createsas");
    }
}
