//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{

    public enum CopyType
    {
        Text = 0,
        TextWithHeaders = 1,
        JSON = 2,
        CSV = 3,
        INSERT = 4,
        IN = 5,
    }
    /// <summary>
    /// Parameters for the copy results request
    /// </summary>
    public class CopyResults2RequestParams : SubsetParams
    {
        /// <summary>
        /// The type of copy operation to perform.
        /// </summary>
        public CopyType CopyType { get; set; }

        /// <summary>
        /// Whether to include the column headers.
        /// </summary>
        public bool IncludeHeaders { get; set; }

        /// <summary>
        /// The selections.
        /// </summary>
        public TableSelectionRange[] Selections { get; set; }
    }

    /// <summary>
    /// Result for the copy results request
    /// </summary>
    public class CopyResults2RequestResult
    {
    }

    /// <summary>
    /// Copy Results Request
    /// </summary>
    public class CopyResults2Request
    {
        public static readonly RequestType<CopyResults2RequestParams, CopyResults2RequestResult> Type =
            RequestType<CopyResults2RequestParams, CopyResults2RequestResult>.Create("query/copy2");
    }
}