//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Diagnostics;
using Microsoft.SqlTools.EditorServices.Protocol.MessageProtocol;

namespace Microsoft.SqlTools.EditorServices.Protocol.DebugAdapter
{
    public class VariablesRequest
    {
        public static readonly
            RequestType<VariablesRequestArguments, VariablesResponseBody> Type =
            RequestType<VariablesRequestArguments, VariablesResponseBody>.Create("variables");
    }

    [DebuggerDisplay("VariablesReference = {VariablesReference}")]
    public class VariablesRequestArguments
    {
        public int VariablesReference { get; set; }
    }

    public class VariablesResponseBody
    {
        public Variable[] Variables { get; set; }
    }
}

