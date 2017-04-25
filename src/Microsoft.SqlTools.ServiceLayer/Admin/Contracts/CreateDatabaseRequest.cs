//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts
{
    public class CreateDatabaseParams
    {
        DatabaseInfo DatabaseInfo { get; set; }
    }

    public class CreateDatabaseResponse
    {
        bool Result { get; set;  }

        int TaskId { get; set;  }
    }

    public class CreateDatabaseRequest
    {
        public static readonly
            RequestType<CreateDatabaseParams, CreateDatabaseResponse> Type =
                RequestType<CreateDatabaseParams, CreateDatabaseResponse>.Create("admin/createdatabase");
    }
}
