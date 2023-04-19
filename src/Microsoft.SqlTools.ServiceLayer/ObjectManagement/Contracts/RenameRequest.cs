//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class RenameRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// The object type.
        /// </summary>
        public SqlObjectType ObjectType { get; set; }
        /// <summary>
        /// SFC (SMO) URN identifying the object  
        /// </summary>
        public string ObjectUrn { get; set; }
        /// <summary>
        /// the new name of the object
        /// </summary>
        public string NewName { get; set; }
        /// <summary>
        /// Connection uri
        /// </summary>
        public string ConnectionUri { get; set; }
    }

    public class RenameRequestResponse { }

    public class RenameRequest
    {
        public static readonly RequestType<RenameRequestParams, RenameRequestResponse> Type = RequestType<RenameRequestParams, RenameRequestResponse>.Create("objectManagement/rename");
    }
}