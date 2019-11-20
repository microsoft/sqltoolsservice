//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts
{
    public class ShowOnlineHelpRequest
    {
        public static readonly 
            RequestType<string, object> Type = 
            RequestType<string, object>.Create("SqlTools/showOnlineHelp");
    }
}
