//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for a request to get a script with all DML statements for an edit session.
    /// </summary>
    public class EditScriptParams : SessionOperationParams
    {

    }

    /// <summary>
    /// Returns the scripts with all DML statements for an edit session.
    /// </summary>
    public class EditScriptResult
    {
        public string[] Scripts { get; set; }
    }

    public class EditScriptRequest
    {
        public static readonly
            RequestType<EditScriptParams, EditScriptResult> Type =
            RequestType<EditScriptParams, EditScriptResult>.Create("edit/script");
    }
}
