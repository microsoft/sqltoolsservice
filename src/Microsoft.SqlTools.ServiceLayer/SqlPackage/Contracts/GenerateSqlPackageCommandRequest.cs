//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.Tools.Schema.CommandLineTool;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts
{
    /// <summary>
    /// Request to generate SqlPackage command based on action
    /// </summary>
    public class GenerateSqlPackageCommandRequest
    {
        public static readonly RequestType<GenerateSqlPackageCommandParams, SqlPackageCommandResult> Type =
            RequestType<GenerateSqlPackageCommandParams, SqlPackageCommandResult>.Create("sqlpackage/generateCommand");
    }
}
