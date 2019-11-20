//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for the update cell request
    /// </summary>
    public class EditCreateRowParams : SessionOperationParams
    {
    }

    /// <summary>
    /// Parameters to return upon successful addition of a row to the edit session
    /// </summary>
    public class EditCreateRowResult
    {
        /// <summary>
        /// Each element corresponds to the default value for the column. If null, no default is
        /// defined. Calculated columns will have placeholders provided here.
        /// </summary>
        public string[] DefaultValues { get; set; }

        /// <summary>
        /// The internal ID of the newly created row
        /// </summary>
        public long NewRowId { get; set; }
    }

    public class EditCreateRowRequest
    {
        public static readonly
            RequestType<EditCreateRowParams, EditCreateRowResult> Type =
            RequestType<EditCreateRowParams, EditCreateRowResult>.Create("edit/createRow");

    }
}
