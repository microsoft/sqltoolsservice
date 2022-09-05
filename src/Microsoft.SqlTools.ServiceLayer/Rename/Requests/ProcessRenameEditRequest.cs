//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Rename.Requests
{
    /// <summary>
    /// Property class for Rename Service
    /// </summary>
    public class ProcessRenameEditRequestParams : GeneralRequestDetails
    {
        public RenameTableInfo TableInfo { get; set; }
        public RenameTableChangeInfo ChangeInfo { get; set; }
    }
    public class ProcessRenameEditRequest
    {
        public static readonly RequestType<ProcessRenameEditRequestParams, bool> Type = RequestType<ProcessRenameEditRequestParams, bool>.Create("rename/processedit");
    }
}