//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts
{
    /// <summary>
    /// Params for a get database info request
    /// </summar>
    public class GetAssociatedFilesParams
    {
        /// <summary>
        /// URI identifier for the connection to get the server folder info for
        /// </summary>
        public string ConnectionUri { get; set; }
        /// <summary>
        /// The file path for the primary file that we want to get the associated files for.
        /// </summary>
        public string PrimaryFilePath { get; set; }
    }

    /// <summary>
    /// Get database info request mapping
    /// </summary>
    public class GetAssociatedFilesRequest
    {
        public static readonly
            RequestType<GetAssociatedFilesParams, string[]> Type =
                RequestType<GetAssociatedFilesParams, string[]>.Create("admin/getassociatedfiles");
    }
}
