//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities
{
    public class WorkspaceCapabilities
    {
        /// <summary>
        /// Options specific to workspace folders the server supports
        /// </summary>
        public WorkspaceFolderCapabilities WorkspaceFolders { get; set; }
    }
    
    public class WorkspaceFolderCapabilities
    {
        /// <summary>
        /// Whether or not the server supports multiple workspace folders
        /// </summary>
        public bool? Supported { get; set; }
        
        /// <summary>
        /// Whether the server wants to receive workspace folder change notifications
        /// </summary>
        /// <remarks>
        /// If a string is provided, the string is treated as an ID under which the notification is
        /// registered on the client side. The ID can be used to unregister for these events using
        /// the client/unregisterCapability request
        /// </remarks>
        public WorkspaceChangeNotification ChangeNotifications { get; set; }
    }

    [JsonConverter(typeof(WorkspaceChangeNotificationJsonConverter))]
    public class WorkspaceChangeNotification
    {
        public bool BooleanValue { get; }
        
        public string UnregisterId { get; }

        private WorkspaceChangeNotification(bool booleanValue, string unregisterId)
        {
            BooleanValue = booleanValue;
            UnregisterId = unregisterId;
        }

        /// <summary>
        /// Indicates that this server can accept notifications that the workspace has changed
        /// </summary>
        public static WorkspaceChangeNotification True => new WorkspaceChangeNotification(true, null);

        /// <summary>
        /// Indicates that this server cannot accept notifications that the workspace has changed
        /// </summary>
        public static WorkspaceChangeNotification False => new WorkspaceChangeNotification(false, null);

        /// <summary>
        /// Indicates that this server can accept notifications that the workspace has changed but
        /// reserves the right to unsubscribe from receiving workspace change notifications
        /// </summary>
        /// <param name="unregisterId">ID to use when unregistering for workspace change notifications</param>
        public static WorkspaceChangeNotification WithId(string unregisterId)
        {
            return unregisterId == null ? null : new WorkspaceChangeNotification(true, unregisterId);
        }
    }
    
    internal class WorkspaceChangeNotificationJsonConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {           
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            WorkspaceChangeNotification castValue = (WorkspaceChangeNotification) value;
            if (castValue.UnregisterId != null)
            {
                writer.WriteValue(castValue.UnregisterId);
            }
            else
            {
                writer.WriteValue(castValue.BooleanValue);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken jToken = JToken.Load(reader);
            if (jToken.Type == JTokenType.Null)
            {
                return null;
            }
            if (jToken.Type == JTokenType.Boolean)
            {
                return jToken.Value<bool>()
                    ? WorkspaceChangeNotification.True
                    : WorkspaceChangeNotification.False;
            }

            return WorkspaceChangeNotification.WithId(jToken.Value<string>());
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(WorkspaceChangeNotificationJsonConverter);
        }
    }
}