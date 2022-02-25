//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Backup parameters passed for execution and scripting
    /// </summary>
    public class CreateSasParams
    {
        /// <summary>
        /// Blob container URI
        /// </summary>
        public string BlobContainerUri { get; set; }

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
                RequestType<CreateSasParams, CreateSasResponse>.Create("backup/createsas");
    }
}
