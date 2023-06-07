//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class InitializeViewRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// The connection uri.
        /// </summary>
        public string ConnectionUri { get; set; }
        /// <summary>
        /// The target database name.
        /// </summary>
        public string Database { get; set; }
        /// <summary>
        /// The object type.
        /// </summary>
        public SqlObjectType ObjectType { get; set; }
        /// <summary>
        /// Whether the view is for a new object.
        /// </summary>
        public bool IsNewObject { get; set; }
        /// <summary>
        /// The object view context id.
        /// </summary>
        public string ContextId { get; set; }
        /// <summary>
        /// Urn of the parent object.
        /// </summary>
        public string ParentUrn { get; set; }
        /// <summary>
        /// Urn of the object. Only set when the view is for an existing object.
        /// </summary>
        public string ObjectUrn { get; set; }
        /// <summary>
        /// Type of the dialog that is currently processing ex:Edit/New/Properties
        /// </summary>
        public string DialogType { get; set; }
    }

    public class InitializeViewRequest
    {
        public static readonly RequestType<InitializeViewRequestParams, SqlObjectViewInfo> Type = RequestType<InitializeViewRequestParams, SqlObjectViewInfo>.Create("objectManagement/initializeView");
    }
}