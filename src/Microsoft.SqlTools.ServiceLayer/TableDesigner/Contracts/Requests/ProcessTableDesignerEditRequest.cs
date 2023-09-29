//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.SqlCore.TableDesigner.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public class ProcessTableDesignerEditRequestParams : GeneralRequestDetails
    {
        public TableInfo TableInfo { get; set; }

        public TableDesignerChangeInfo TableChangeInfo { get; set; }
    }

    /// <summary>
    /// The service request to process the changes made in the table designer.
    /// </summary>
    public class ProcessTableDesignerEditRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly RequestType<ProcessTableDesignerEditRequestParams, ProcessTableDesignerEditResponse> Type = RequestType<ProcessTableDesignerEditRequestParams, ProcessTableDesignerEditResponse>.Create("tabledesigner/processedit");
    }
}
