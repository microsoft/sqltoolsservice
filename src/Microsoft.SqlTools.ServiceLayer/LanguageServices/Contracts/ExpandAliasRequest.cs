﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    public class ExpandAliasRequest
    {
        public static readonly
            RequestType<string, string> Type =
            RequestType<string, string>.Create("SqlTools/expandAlias");
    }
}
