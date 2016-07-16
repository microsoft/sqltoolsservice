//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.EditorServices.Protocol.MessageProtocol;

namespace Microsoft.SqlTools.EditorServices.Protocol.DebugAdapter
{
    public class ConfigurationDoneRequest
    {
        public static readonly
            RequestType<object, object> Type =
            RequestType<object, object>.Create("configurationDone");
    }
}
