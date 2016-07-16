//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.EditorServices.Protocol.MessageProtocol;

namespace Microsoft.SqlTools.EditorServices.Protocol.DebugAdapter
{
    public class OutputEvent
    {
        public static readonly
            EventType<OutputEventBody> Type =
            EventType<OutputEventBody>.Create("output");
    }

    public class OutputEventBody
    {
        public string Category { get; set; }

        public string Output { get; set; }
    }
}

