//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Contracts.Internal;
using Microsoft.SqlTools.Hosting.v2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    /// <summary>
    /// Defines all possible message types.
    /// </summary>
    public enum MessageType
    {
        Request,
        Response,
        ResponseError,
        Event
    }

    /// <summary>
    /// Representation for a JSON RPC message. Provides logic for converting back and forth from
    /// string
    /// </summary>
    [DebuggerDisplay("MessageType = {MessageType.ToString()}, Method = {Method}, Id = {Id}")]
    public class Message
    {
        #region Constants
        
        private const string JsonRpcVersion = "2.0";
        
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        private static readonly JsonSerializer ContentSerializer = JsonSerializer.Create(JsonSerializerSettings);
        
        #endregion
        
        #region Construction

        private Message(MessageType messageType, JToken contents)
        {
            MessageType = messageType;
            Contents = contents;
        }

        /// <summary>
        /// Creates a message with a Request type
        /// </summary>
        /// <param name="requestType">Configuration for the request</param>
        /// <param name="id">ID of the message</param>
        /// <param name="contents">Contents of the message</param>
        /// <typeparam name="TParams">Type of the contents of the message</typeparam>
        /// <typeparam name="TResult">Type of the contents of the results to this request</typeparam>
        /// <returns>Message with a Request type</returns>
        public static Message CreateRequest<TParams, TResult>(
            RequestType<TParams, TResult> requestType, 
            string id,
            TParams contents)
        {
            JToken contentsToken = contents == null ? null : JToken.FromObject(contents, ContentSerializer);
            return new Message(MessageType.Request, contentsToken)
            {
                Id = id,
                Method = requestType.MethodName
            };
        }

        /// <summary>
        /// Creates a message with a Response type.
        /// </summary>
        /// <param name="id">The sequence ID of the original request.</param>
        /// <param name="contents">The contents of the response.</param>
        /// <returns>A message with a Response type.</returns>
        public static Message CreateResponse<TParams>(
            string id, 
            TParams contents)
        {
            JToken contentsToken = contents == null ? null : JToken.FromObject(contents, ContentSerializer);
            return new Message(MessageType.Response, contentsToken)
            {
                Id = id
            };
        }

        /// <summary>
        /// Creates a message with a Response type and error details.
        /// </summary>
        /// <param name="id">The sequence ID of the original request.</param>
        /// <param name="error">The error details of the response.</param>
        /// <returns>A message with a Response type and error details.</returns>
        public static Message CreateResponseError(
            string id, 
            Error error)
        {
            JToken errorToken = error == null ? null : JToken.FromObject(error, ContentSerializer);
            return new Message(MessageType.ResponseError, errorToken)
            {
                Id = id
            };
        }

        /// <summary>
        /// Creates a message with an Event type.
        /// </summary>
        /// <param name="eventType">Configuration for the event message</param>
        /// <param name="contents">The contents of the event.</param>
        /// <typeparam name="TParams"></typeparam>
        /// <returns>A message with an Event type</returns>
        public static Message CreateEvent<TParams>(
            EventType<TParams> eventType, 
            TParams contents)
        {
            JToken contentsToken = contents == null ? null : JToken.FromObject(contents, ContentSerializer);
            return new Message(MessageType.Event, contentsToken)
            {
                Method = eventType.MethodName
            };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public MessageType MessageType { get; }

        /// <summary>
        /// Gets or sets the message's sequence ID.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets or sets the message's method/command name.
        /// </summary>
        public string Method { get; private set; }

        /// <summary>
        /// Gets or sets a JToken containing the contents of the message.
        /// </summary>
        public JToken Contents { get; }
        
        #endregion
        
        #region Serialization/Deserialization
        
        public static Message Deserialize(string jsonString)
        {
            // Deserialize the object from the JSON into an intermediate object
            JObject messageObject = JObject.Parse(jsonString);
            JToken token;
            
            // Ensure there's a JSON RPC version or else it's invalid
            if (!messageObject.TryGetValue("jsonrpc", out token) || token.Value<string>() != JsonRpcVersion)
            {
                throw new MessageParseException(null, SR.HostingJsonRpcVersionMissing);
            }

            if (messageObject.TryGetValue("id", out token))
            {
                // Message with ID is a Request or Response
                string messageId = token.ToString();

                if (messageObject.TryGetValue("result", out token))
                {
                    return new Message(MessageType.Response, token) {Id = messageId};
                }
                if (messageObject.TryGetValue("error", out token))
                {
                    return new Message(MessageType.ResponseError, token) {Id = messageId};
                }
                
                // Message without result/error is a Request
                JToken messageParams;
                messageObject.TryGetValue("params", out messageParams);
                if (!messageObject.TryGetValue("method", out token))
                {
                    throw new MessageParseException(null, SR.HostingMessageMissingMethod);
                }

                return new Message(MessageType.Request, messageParams) {Id = messageId, Method = token.ToString()};
            }
            else
            {
                // Messages without an id are events
                JToken messageParams;
                messageObject.TryGetValue("params", out messageParams);

                if (!messageObject.TryGetValue("method", out token))
                {
                    throw new MessageParseException(null, SR.HostingMessageMissingMethod);
                }

                return new Message(MessageType.Event, messageParams) {Method = token.ToString()};
            }
        }
        
        public string Serialize()
        {
            JObject messageObject = new JObject
            {
                {"jsonrpc", JToken.FromObject(JsonRpcVersion)}
            };

            switch (MessageType)
            {
                case MessageType.Request:
                    messageObject.Add("id", JToken.FromObject(Id));
                    messageObject.Add("method", Method);
                    messageObject.Add("params", Contents);
                    break;
                case MessageType.Event:
                    messageObject.Add("method", Method);
                    messageObject.Add("params", Contents);
                    break;
                case MessageType.Response:
                    messageObject.Add("id", JToken.FromObject(Id));
                    messageObject.Add("result", Contents);
                    break;
                case MessageType.ResponseError:
                    messageObject.Add("id", JToken.FromObject(Id));
                    messageObject.Add("error", Contents);
                    break;
            }

            return JsonConvert.SerializeObject(messageObject);
        }

        public TContents GetTypedContents<TContents>()
        {
            TContents typedContents = default(TContents);
            if (Contents != null)
            {
                typedContents = Contents.ToObject<TContents>();
            }

            return typedContents;
        }
        
        #endregion
    }
}
