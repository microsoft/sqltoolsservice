//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.AzureBlob.Contracts
{
    /// <summary>
    /// Parameters passed for creating shared access signature
    /// </summary>
    public class CreateSasParams
    {
        /// <summary>
        /// Connection URI
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
        /// <summary>
        /// Shared access signature expiration date
        /// </summary>
        public string ExpirationDate { get; set; }
    }

    /// <summary>
    /// Response class for creating shared access signature
    /// </summary>
    public class CreateSasResponse
    {
        public string SharedAccessSignature { get; set; }

    }

    /// <summary>
    /// Request class for creating shared access signature
    /// </summary>
    public class CreateSasRequest
    {
        public static readonly
            RequestType<CreateSasParams, CreateSasResponse> Type =
                RequestType<CreateSasParams, CreateSasResponse>.Create("blob/createSas");
    }
}
