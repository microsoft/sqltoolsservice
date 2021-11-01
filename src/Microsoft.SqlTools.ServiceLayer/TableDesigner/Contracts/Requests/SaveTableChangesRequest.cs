//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public class SaveTableChangesRequestParams : GeneralRequestDetails
    {
        public TableInfo TableInfo { get; set; }

        public TableViewModel ViewModel { get; set; }
    }

    public class SaveTableChangesResponse
    {
    }

    /// <summary>
    /// The service request to save the changes.
    /// </summary>
    public class SaveTableChangesRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly RequestType<SaveTableChangesRequestParams, SaveTableChangesResponse> Type = RequestType<SaveTableChangesRequestParams, SaveTableChangesResponse>.Create("tabledesigner/savechanges");
    }
}
