//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Workspace
{
    public class DidChangeConfigurationNotification<TConfig> 
    {
        public static readonly
            EventType<DidChangeConfigurationParams<TConfig>> Type =
            EventType<DidChangeConfigurationParams<TConfig>>.Create("workspace/didChangeConfiguration");
    }

    public class DidChangeConfigurationParams<TConfig>
    {
        public TConfig Settings { get; set; }
    }
}
