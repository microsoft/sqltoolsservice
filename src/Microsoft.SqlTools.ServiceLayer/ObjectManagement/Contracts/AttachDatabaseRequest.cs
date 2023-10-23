//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class DatabaseFileData
    {
        public string DatabaseName { get; set; }
        public string[] DatabaseFilePaths { get; set; }
        public string Owner { get; set; }
    }

    public class AttachDatabaseRequestParams
    {
        public string ConnectionUri { get; set; }
        public DatabaseFileData[] Databases { get; set; }
        public bool GenerateScript { get; set; }
    }

    public class AttachDatabaseRequest
    {
        public static readonly RequestType<AttachDatabaseRequestParams, string> Type = RequestType<AttachDatabaseRequestParams, string>.Create("objectManagement/attachDatabase");
    }
}   