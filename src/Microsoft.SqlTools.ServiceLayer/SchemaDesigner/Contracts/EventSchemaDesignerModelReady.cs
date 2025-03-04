//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerModelReadyParams
    {
        public string SessionId;
    }

    public class SchemaDesignerModelReady
    {
        public static readonly
            EventType<SchemaDesignerModelReadyParams> Type =
            EventType<SchemaDesignerModelReadyParams>.Create("schemaDesigner/modelReady");
    }

}