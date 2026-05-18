//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerProgressNotificationParams
    {
        public string SessionId { get; set; } = null!;
        public string Operation { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    public class SchemaDesignerProgressNotification
    {
        public static readonly EventType<SchemaDesignerProgressNotificationParams> Type =
            EventType<SchemaDesignerProgressNotificationParams>.Create("schemaDesigner/progress");
    }

    public class SchemaDesignerMessageNotificationParams
    {
        public string SessionId { get; set; } = null!;
        public string Operation { get; set; } = null!;
        public string MessageType { get; set; } = null!;
        public string Message { get; set; } = null!;
        public int Number { get; set; }
        public string? Prefix { get; set; }
        public double? Progress { get; set; }
        public string? SchemaName { get; set; }
        public string? TableName { get; set; }
    }

    public class SchemaDesignerMessageNotification
    {
        public static readonly EventType<SchemaDesignerMessageNotificationParams> Type =
            EventType<SchemaDesignerMessageNotificationParams>.Create("schemaDesigner/message");
    }
}