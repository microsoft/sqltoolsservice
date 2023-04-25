//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Exclude a folder and its contents from a project
    /// </summary>
    public class ExcludeFolderRequest
    {
        public static readonly RequestType<FolderParams, ResultStatus> Type = RequestType<FolderParams, ResultStatus>.Create("sqlProjects/excludeFolder");
    }
}
