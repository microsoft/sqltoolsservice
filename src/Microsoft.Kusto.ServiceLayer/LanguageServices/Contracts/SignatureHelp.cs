//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts
{
    public class SignatureHelpRequest
    {
        public static readonly
            RequestType<TextDocumentPosition, SignatureHelp> Type =
            RequestType<TextDocumentPosition, SignatureHelp>.Create("kusto/textDocument/signatureHelp");
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

