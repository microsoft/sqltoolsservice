//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters of the edit session dispose request
    /// </summary>
    public class EditDisposeParams
    {
        /// <summary>
        /// The owner URI for the session to dispose
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Object to return upon successful disposal of an edit session
    /// </summary>
    public class EditDisposeResult { }

    public class EditDisposeRequest
    {
        public static readonly
            RequestType<EditDisposeParams, EditDisposeResult> Type =
            RequestType<EditDisposeParams, EditDisposeResult>.Create("edit/dispose");
    }
}
