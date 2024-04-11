//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Migration.Contracts
{
    public class GetArmTemplateRequest
    {
        public static readonly
            RequestType<string, List<string>> Type =
                RequestType<string, List<string>>.Create("migration/getarmtemplate");
    }
}
