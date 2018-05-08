//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters of the edit session initialize request
    /// </summary>
    public class EditInitializeParams : SessionOperationParams
    {
        /// <summary>
        /// Filtering parameters
        /// </summary>
        public EditInitializeFiltering Filters { get; set; }

        /// <summary>
        /// The object to use for generating an edit script
        /// </summary>
        public string ObjectName { get; set; }

        /// <summary>
        /// The schema for the object to use 
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// The type of the object to use for generating an edit script
        /// </summary>
        public string ObjectType { get; set; }

        /// <summary>
        /// The query used to retrieve result set
        /// </summary>
        public string QueryString { get; set; }
    }

    /// <summary>
    /// Object to return upon successful completion of an edit session initialize request
    /// </summary>
    /// <remarks>
    /// Empty for now, since there isn't anything special to return on success
    /// </remarks>
    public class EditInitializeResult
    {
    }

    public class EditInitializeRequest
    {
        public static readonly 
            RequestType<EditInitializeParams, EditInitializeResult> Type =
            RequestType<EditInitializeParams, EditInitializeResult>.Create("edit/initialize");
    }
}
