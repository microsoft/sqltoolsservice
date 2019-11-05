//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts
{
    public class CreateDatabaseParams
    {
        public string OwnerUri { get; set; }

        public DatabaseInfo DatabaseInfo { get; set; }
    }

    public class CreateDatabaseResponse
    {
        public bool Result { get; set;  }

        public int TaskId { get; set;  }
    }

    public class CreateDatabaseRequest
    {
        public static readonly
            RequestType<CreateDatabaseParams, CreateDatabaseResponse> Type =
                RequestType<CreateDatabaseParams, CreateDatabaseResponse>.Create("admin/createdatabase");
    }
}
