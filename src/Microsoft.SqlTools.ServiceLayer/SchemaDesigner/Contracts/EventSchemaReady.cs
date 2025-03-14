//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaReadyResponse
    {
        public string SessionId;
    }

    public class SchemaReady
    {
        /// <summary>
        /// Event to signal that the DACFx model is loaded and ready
        /// </summary>
        public static readonly
            EventType<SchemaReadyResponse> Type =
            EventType<SchemaReadyResponse>.Create("schemaDesigner/schemaReady");
    }

}