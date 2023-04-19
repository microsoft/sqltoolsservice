//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class DropRequestParams : GeneralRequestDetails
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
        /// Connection uri
        /// </summary>
        public string ConnectionUri { get; set; }
        /// <summary>
        /// Whether to throw an error if the object does not exist. The default value is false.
        /// </summary>
        public bool ThrowIfNotExist { get; set; } = false;
    }

    public class DropRequestResponse { }

    public class DropRequest
    {
        public static readonly RequestType<DropRequestParams, DropRequestResponse> Type = RequestType<DropRequestParams, DropRequestResponse>.Create("objectManagement/drop");
    }
}