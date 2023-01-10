//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class SqlProjectParams : GeneralRequestDetails
    {
        public string ProjectUri { get; set; }
    }

    public class SqlProjectScriptParams : SqlProjectParams
    {
        public string Path { get; set; }
    }

    public class SqlProjectResult
    {
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
