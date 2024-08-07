//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    public class SignatureHelpRequest
    {
        public static readonly
            RequestType<TextDocumentPosition, SignatureHelp> Type =
            RequestType<TextDocumentPosition, SignatureHelp>.Create("textDocument/signatureHelp");
    }

    public class ParameterInformation
    {
        public string Label { get; set; }

        public string Documentation { get; set; }
    }

    public class SignatureInformation
    {
        public string Label { get; set; }

        public string Documentation { get; set; }

        public ParameterInformation[] Parameters { get; set; }
    }

    public class SignatureHelp
    {
        public SignatureInformation[] Signatures { get; set; }

        public int? ActiveSignature { get; set; }

        public int? ActiveParameter { get; set; }
    }
}

