//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    /// <summary>
    /// Notification sent when the schema model is ready
    /// </summary>
    public class ModelReadyParams
    {
        /// <summary>
        /// Gets or sets the session id
        /// </summary>
        public string SessionId { get; set; }

    }

    public class ModelReadyNotification
    {
        public static readonly
            EventType<ModelReadyParams> Type =
            EventType<ModelReadyParams>.Create("schemaDesigner/modelReady");
    }
}