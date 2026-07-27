//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.Hosting.Protocol.Contracts
{
    /// <summary>
    /// Represents a JSON-RPC message ID.
    /// </summary>
    public readonly struct MessageId : IEquatable<MessageId>
    {
        public MessageId(long number)
        {
            this.Number = number;
            this.String = null;
            this.IsNull = false;
        }

        public MessageId(string id)
        {
            this.Number = null;
            this.String = id;
            this.IsNull = id == null;
        }

        public static MessageId NotSpecified => default(MessageId);

        public static MessageId Null => new MessageId(null);

        public long? Number { get; }

        public string String { get; }

        public bool IsNull { get; }

        public bool IsEmpty => this.Number == null && this.String == null && !this.IsNull;

        public static implicit operator MessageId(int id)
        {
            return new MessageId(id);
        }

        public static implicit operator MessageId(long id)
        {
            return new MessageId(id);
        }

        public static implicit operator MessageId(string id)
        {
            return new MessageId(id);
        }

        public static bool operator ==(MessageId first, MessageId second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(MessageId first, MessageId second)
        {
            return !first.Equals(second);
        }

        public static MessageId Parse(JToken token)
        {
            if (token == null)
            {
                return MessageId.NotSpecified;
            }

            switch (token.Type)
            {
                case JTokenType.Integer:
                    return new MessageId(token.ToObject<long>());
                case JTokenType.String:
                    return new MessageId(token.ToObject<string>());
                case JTokenType.Null:
                    return MessageId.Null;
                default:
                    throw new JsonSerializationException("Unexpected token type for message ID: " + token.Type);
            }
        }

        public JToken ToJToken()
        {
            if (this.Number.HasValue)
            {
                return JToken.FromObject(this.Number.Value);
            }

            if (this.String != null)
            {
                return JToken.FromObject(this.String);
            }

            return JValue.CreateNull();
        }

        public bool Equals(MessageId other)
        {
            return this.Number == other.Number &&
                string.Equals(this.String, other.String, StringComparison.Ordinal) &&
                this.IsNull == other.IsNull;
        }

        public override bool Equals(object obj)
        {
            return obj is MessageId other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Number?.GetHashCode() ??
                (this.String != null ? StringComparer.Ordinal.GetHashCode(this.String) : 0);
        }

        public override string ToString()
        {
            return this.Number?.ToString(CultureInfo.InvariantCulture) ??
                this.String ??
                (this.IsNull ? "(null)" : "(not specified)");
        }
    }
}
