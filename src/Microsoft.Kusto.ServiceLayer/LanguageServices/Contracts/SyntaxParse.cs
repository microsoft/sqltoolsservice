//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts
{
    public class SyntaxParseParams 
    {
        public string OwnerUri { get; set; }
        public string Query { get; set; }
    }
    
    public class SyntaxParseResult
    {
        public bool Parseable { get; set; }

        public string[] Errors { get; set; }
    }

    public class SyntaxParseRequest
    {
        public static readonly
            RequestType<SyntaxParseParams, SyntaxParseResult> Type =
            RequestType<SyntaxParseParams, SyntaxParseResult>.Create("query/syntaxparse");
    }
}

