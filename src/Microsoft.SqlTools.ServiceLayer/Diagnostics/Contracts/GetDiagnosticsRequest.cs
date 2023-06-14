//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Diagnostics
{
    public class GetDiagnosticsParams
    {
        public int errorCode { get; set; }

        public string errorMessage { get; set; }

        public string messageDetails { get; set; }
    }

    public class GetDiagnosticsResult
    {
        public string recommendation { get; set; }
    }

    public class GetDiagnosticsRequest
    {
        public static readonly
        RequestType<GetDiagnosticsParams, GetDiagnosticsResult> Type = 
         RequestType<GetDiagnosticsParams, GetDiagnosticsResult>.Create("diagnostics/getDiagnostics");
    }
}
