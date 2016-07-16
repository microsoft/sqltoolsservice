//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.EditorServices.Protocol.MessageProtocol;

namespace Microsoft.SqlTools.EditorServices.Protocol.DebugAdapter
{
    public class StartedEvent
    {
        public static readonly
            EventType<object> Type =
            EventType<object>.Create("started");
    }
}
