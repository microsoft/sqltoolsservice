//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters of the edit session initialize request
    /// </summary>
    public class EditInitializeParams
    {
        /// <summary>
        /// The owner URI for the query to base this edit session on. This URI will also be used
        /// for identifying the edit session later.
        /// </summary>
        public string OwnerUri { get; set; }
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
