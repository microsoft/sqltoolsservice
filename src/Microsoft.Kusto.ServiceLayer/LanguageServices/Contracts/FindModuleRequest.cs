//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts
{
    public class FindModuleRequest
    {
        public static readonly
            RequestType<List<PSModuleMessage>, object> Type =
            RequestType<List<PSModuleMessage>, object>.Create("kusto/SqlTools/findModule");
    }


    public class PSModuleMessage
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
