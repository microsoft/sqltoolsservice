//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.Dmp.Contracts;
using Microsoft.SqlTools.Dmp.Contracts.Hosting;
using Microsoft.SqlTools.Dmp.Hosting.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.Dmp.Hosting.UnitTests.ProtocolTests
{
    public static class Common
    {
        public const string MessageId = "123";
        
        public static readonly RequestType<TestMessageContents, TestMessageContents> RequestType = 
            RequestType<TestMessageContents, TestMessageContents>.Create("test/request");

        public static readonly EventType<TestMessageContents> EventType = 
            EventType<TestMessageContents>.Create("test/event");
        
        public static readonly Message RequestMessage =
            Message.CreateRequest(RequestType, MessageId, TestMessageContents.DefaultInstance);

        public static readonly Message ResponseMessage =
            Message.CreateResponse(MessageId, TestMessageContents.DefaultInstance);
        
        public static readonly Message EventMessage =
            Message.CreateEvent(EventType, TestMessageContents.DefaultInstance);

        public static class TestErrorContents
        {
            public static readonly JToken SerializedContents = JToken.Parse("{\"code\": 123, \"message\": \"error\"}");
            public static readonly Error DefaultInstance = new Error {Code = 123, Message = "error"};
        }
        
        public class TestMessageContents : IEquatable<TestMessageContents>
        {
            public const string JsonContents = "{\"someField\": \"Some value\", \"number\": 42}";
            public static readonly JToken SerializedContents = JToken.Parse(JsonContents);
            public static readonly TestMessageContents DefaultInstance = new TestMessageContents {Number = 42, SomeField = "Some value"};

            public string SomeField { get; set; }
            public int Number { get; set; }

            public bool Equals(TestMessageContents other)
            {
                return string.Equals(SomeField, other.SomeField) 
                       && Number == other.Number;
            }

            public static bool operator ==(TestMessageContents obj1, TestMessageContents obj2)
            {
                bool bothNull = ReferenceEquals(obj1, null) && ReferenceEquals(obj2, null);
                bool someNull = ReferenceEquals(obj1, null) || ReferenceEquals(obj2, null);
                return bothNull || !someNull && obj1.Equals(obj2);
            }

            public static bool operator !=(TestMessageContents obj1, TestMessageContents obj2)
            {
                return !(obj1 == obj2);
            }
        }
    }
}