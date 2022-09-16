//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Requests
{
    /// <summary>
    /// Property class for ObjectManagement Service
    /// </summary>
    public class RenameRequestParams : GeneralRequestDetails
    {
        public string UrnOfObject { get; set; }
        public string NewName { get; set; }
        public string OwnerUri { get; set; }
    }
    public class RenameRequest
    {
        public static readonly RequestType<RenameRequestParams, bool> Type = RequestType<RenameRequestParams, bool>.Create("objectmanagement/rename");
    }
}