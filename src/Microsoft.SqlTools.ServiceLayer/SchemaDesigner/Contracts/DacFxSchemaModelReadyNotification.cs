//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class ModelReadyParams
    {
       public SchemaModel Model;
       public SchemaModel OriginalModel;
    }
    public class ModelReadyNotification
    {
        public static readonly
            EventType<ModelReadyParams> Type =
            EventType<ModelReadyParams>.Create("schemaDesigner/modelReady");
    }

}