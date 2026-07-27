//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    [DataContract]
    public class TableDesignerProgressNotificationParams
    {
        [DataMember(Name = "sessionId")]
        public string SessionId { get; set; } = null!;

        [DataMember(Name = "operation")]
        public string Operation { get; set; } = null!;

        [DataMember(Name = "status")]
        public string Status { get; set; } = null!;

        [DataMember(Name = "message")]
        public string Message { get; set; } = null!;
    }

    public class TableDesignerProgressNotification
    {
        public static readonly EventType<TableDesignerProgressNotificationParams> Type =
            EventType<TableDesignerProgressNotificationParams>.Create("tabledesigner/progress");
    }

    [DataContract]
    public class TableDesignerMessageNotificationParams
    {
        [DataMember(Name = "sessionId")]
        public string SessionId { get; set; } = null!;

        [DataMember(Name = "operation")]
        public string Operation { get; set; } = null!;

        [DataMember(Name = "messageType")]
        public string MessageType { get; set; } = null!;

        [DataMember(Name = "message")]
        public string Message { get; set; } = null!;

        [DataMember(Name = "number")]
        public int Number { get; set; }

        [DataMember(Name = "prefix")]
        public string Prefix { get; set; } = null!;

        [DataMember(Name = "progress")]
        public double? Progress { get; set; }

        [DataMember(Name = "schemaName")]
        public string SchemaName { get; set; } = null!;

        [DataMember(Name = "tableName")]
        public string TableName { get; set; } = null!;
    }

    public class TableDesignerMessageNotification
    {
        public static readonly EventType<TableDesignerMessageNotificationParams> Type =
            EventType<TableDesignerMessageNotificationParams>.Create("tabledesigner/message");
    }
}
