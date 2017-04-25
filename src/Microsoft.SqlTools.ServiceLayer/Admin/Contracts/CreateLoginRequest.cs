//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts
{
    public class CreateLoginParams
    {
        LoginInfo DatabaseInfo { get; set; }
    }

    public class CreateLoginResponse
    {
        bool Result { get; set; }

        int TaskId { get; set; }
    }

    public class CreateLoginRequest
    {
        public static readonly
            RequestType<CreateLoginParams, CreateLoginResponse> Type =
                RequestType<CreateLoginParams, CreateLoginResponse>.Create("admin/createlogin");
    }
}
