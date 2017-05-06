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
    public class AdminServiceOptionsParams
    {
        public string OwnerUri { get; set; }
    }

    public class AdminServiceOptionsResponse
    {
        public DatabaseInfo DefaultDatabaseInfo { get; set;  }
    }

    public class AdminServiceOptionsRequest
    {
        public static readonly
            RequestType<AdminServiceOptionsParams, AdminServiceOptionsResponse> Type =
                RequestType<AdminServiceOptionsParams, AdminServiceOptionsResponse>.Create("admin/options");
    }
}
