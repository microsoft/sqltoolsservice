//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts
{
    /// <summary>
    /// Command-line arguments for SqlPackage operations containing source/target paths, connection strings, etc.
    /// Inherits from DacFx CommandLineArguments to avoid duplication and enable direct usage with SqlPackage APIs.
    /// </summary>
    public class SqlPackageCommandLineArguments : Microsoft.Data.Tools.Schema.CommandLineTool.CommandLineArguments
    {
    }
}
