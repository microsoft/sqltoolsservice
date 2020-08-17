//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Workspace.Contracts
{
    public class DidChangeConfigurationNotification<TConfig> 
    {
        public static readonly
            EventType<DidChangeConfigurationParams<TConfig>> Type =
            EventType<DidChangeConfigurationParams<TConfig>>.Create("kusto/workspace/didChangeConfiguration");
    }

    public class DidChangeConfigurationParams<TConfig>
    {
        public TConfig Settings { get; set; }
    }
}
