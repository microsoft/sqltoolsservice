﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Migration.Contracts
{
    public class GetArmTemplateRequest
    {
        public static readonly
            RequestType<string, string> Type =
                RequestType<string, string>.Create("migration/getarmtemplate");
    }
}
