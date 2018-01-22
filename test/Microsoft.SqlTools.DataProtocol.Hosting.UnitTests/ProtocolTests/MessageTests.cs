//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.DataProtocol.Contracts;
using Microsoft.SqlTools.DataProtocol.Hosting.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.SqlTools.DataProtocol.Hosting.UnitTests.ProtocolTests
{
    public class MessageTests
    {
        #region Construction/Serialization Tests
        
        [Fact]
        public void CreateRequest()
        {
            // If: I create a request
            var message = Message.CreateRequest(Common.RequestType, Common.MessageId, Common.TestMessageContents.DefaultInstance);
            
            // Then:
            // ... The message should have all the properties I defined
            // ... The JObject should have the same properties
            var expectedResults = new MessagePropertyResults
            {
                MessageType = MessageType.Request,
                IdSet = true,
                MethodSetAs = Common.RequestType.MethodName,
                ContentsSetAs = "params",
                ErrorSet = false
            };
            AssertPropertiesSet(expectedResults, message);
        }

        [Fact]
        public void CreateError()
        {
            // If: I create an error
            var message = Message.CreateResponseError(Common.MessageId, Common.TestErrorContents.DefaultInstance);
            
            // Then: Message and JObject should have appropriate properties set
            var expectedResults = new MessagePropertyResults
            {
                MessageType = MessageType.ResponseError,
                IdSet = true,
                MethodSetAs = null,
                ContentsSetAs = null,
                ErrorSet = true
            };
            AssertPropertiesSet(expectedResults, message);
        }

        [Fact]
        public void CreateResponse()
        {
            // If: I create a response
            var message = Message.CreateResponse(Common.MessageId, Common.TestMessageContents.DefaultInstance);
            
            // Then: Message and JObject should have appropriate properties set
            var expectedResults = new MessagePropertyResults
            {
                MessageType = MessageType.Response,
                IdSet = true,
                MethodSetAs = null,
                ContentsSetAs = "result",
                ErrorSet = false
            };
            AssertPropertiesSet(expectedResults, message);
        }

        [Fact]
        public void CreateEvent()
        {
            // If: I create an event
            var message = Message.CreateEvent(Common.EventType, Common.TestMessageContents.DefaultInstance);
            
            // Then: Message and JObject should have appropriate properties set
            var expectedResults = new MessagePropertyResults
            {
                MessageType = MessageType.Event,
                IdSet = false,
                MethodSetAs = Common.EventType.MethodName,
                ContentsSetAs = "params",
                ErrorSet = false
            };
            AssertPropertiesSet(expectedResults, message);
        }
        
        private static void AssertPropertiesSet(MessagePropertyResults results, Message message)
        {
            Assert.NotNull(message);
            
            // Serialize the message and deserialize back into a JObject
            string messageJson = message.Serialize();
            Assert.NotNull(messageJson);
            JObject jObject = JObject.Parse(messageJson);
            
            // JSON RPC Version
            List<string> expectedProperties = new List<string> {"jsonrpc"};
            Assert.Equal("2.0", jObject["jsonrpc"]);
            
            // Message Type
            Assert.Equal(results.MessageType, message.MessageType);
            
            // ID
            if (results.IdSet)
            {
                Assert.Equal(Common.MessageId, message.Id);
                Assert.Equal(Common.MessageId, jObject["id"]);
                expectedProperties.Add("id");
            }
            else
            {
                Assert.Null(message.Id);
            }

            // Method
            if (results.MethodSetAs != null)
            {
                Assert.Equal(results.MethodSetAs, message.Method);
                Assert.Equal(results.MethodSetAs, jObject["method"]);
                expectedProperties.Add("method");
            }
            else
            {
                Assert.Null(message.Method);
            }

            // Contents
            if (results.ContentsSetAs != null)
            {
                Assert.Equal(Common.TestMessageContents.SerializedContents, message.Contents);
                Assert.Equal(Common.TestMessageContents.SerializedContents, jObject[results.ContentsSetAs]);
                expectedProperties.Add(results.ContentsSetAs);
            }
            
            // Error
            if (results.ErrorSet)
            {
                Assert.Equal(Common.TestErrorContents.SerializedContents, message.Contents);
                Assert.Equal(Common.TestErrorContents.SerializedContents, jObject["error"]);
                expectedProperties.Add("error");
            }
            
            // Look for any extra properties set in the JObject
            IEnumerable<string> setProperties = jObject.Properties().Select(p => p.Name);
            Assert.Empty(setProperties.Except(expectedProperties));
        }
        
        private class MessagePropertyResults
        {
            public MessageType MessageType { get; set; }
            public bool IdSet { get; set; }
            public string MethodSetAs { get; set; }
            public string ContentsSetAs { get; set; }
            public bool ErrorSet { get; set; }
        }
        
        #endregion
        
        #region Deserialization Tests

        [Fact]
        public void DeserializeMissingJsonRpc()
        {
            // If: I deserialize a json string that doesn't have a JSON RPC version
            // Then: I should get an exception
            Assert.Throws<MessageParseException>(() => Message.Deserialize("{\"id\": 123}"));
        }
        
        [Fact]
        public void DeserializeEvent()
        {
            // If: I deserialize an event json string
            Message m = Message.Deserialize("{\"jsonrpc\": \"2.0\", \"params\": {}, \"method\": \"event\"}");
            
            // Then: I should get an event message back
            Assert.NotNull(m);
            Assert.Equal(MessageType.Event, m.MessageType);
            Assert.Equal("event", m.Method);
            Assert.NotNull(m.Contents);
            Assert.Null(m.Id);
        }

        [Fact]
        public void DeserializeEventMissingMethod()
        {
            // If: I deserialize an event json string that is missing a method
            // Then: I should get an exception
            Assert.Throws<MessageParseException>(() => Message.Deserialize("{\"jsonrpc\": \"2.0\", \"params\": {}}")); 
        }

        [Fact]
        public void DeserializeResponse()
        {
            // If: I deserialize a response json string
            Message m = Message.Deserialize("{\"jsonrpc\": \"2.0\", \"result\": {}, \"id\": \"123\"}");
            
            // Then: I should get a response message back
            Assert.NotNull(m);
            Assert.Equal(MessageType.Response, m.MessageType);
            Assert.Equal("123", m.Id);
            Assert.NotNull(m.Contents);
            Assert.Null(m.Method);
        }

        [Fact]
        public void DeserializeErrorResponse()
        {
            // If: I deserialize an error response
            Message m = Message.Deserialize("{\"jsonrpc\": \"2.0\", \"error\": {}, \"id\": \"123\"}");
            
            // Then: I should get an error response message back
            Assert.NotNull(m);
            Assert.Equal(MessageType.ResponseError, m.MessageType);
            Assert.Equal("123", m.Id);
            Assert.NotNull(m.Contents);
            Assert.Null(m.Method);
        }

        [Fact]
        public void DeserializeRequest()
        {
            // If: I deserialize a request
            Message m = Message.Deserialize("{\"jsonrpc\": \"2.0\", \"params\": {}, \"method\": \"request\", \"id\": \"123\"}");
            
            // Then: I should get a request message back
            Assert.NotNull(m);
            Assert.Equal(MessageType.Request, m.MessageType);
            Assert.Equal("123", m.Id);
            Assert.Equal("request", m.Method);
            Assert.NotNull(m.Contents);
        }

        [Fact]
        public void DeserializeRequestMissingMethod()
        {
            // If: I deserialize a request that doesn't have a method parameter
            // Then: I should get an exception
            Assert.Throws<MessageParseException>(() => Message.Deserialize("{\"jsonrpc\": \"2.0\", \"params\": {}, \"id\": \"123\"}"));
        }

        [Fact]
        public void GetTypedContentsNull()
        {
            // If: I have a message that has a null contents, and I get the typed contents of it
            var m = Message.CreateResponse<Common.TestMessageContents>(Common.MessageId, null);
            var c = m.GetTypedContents<Common.TestMessageContents>();
            
            // Then: I should get null back as the test message contents
            Assert.Null(c);
        }

        [Fact]
        public void GetTypedContentsSimpleValue()
        {
            // If: I have a message that has simple contents, and I get the typed contents of it
            var m = Message.CreateResponse(Common.MessageId, 123);
            var c = m.GetTypedContents<int>();
            
            // Then: I should get an int back
            Assert.Equal(123, c);
        }

        [Fact]
        public void GetTypedContentsClassValue()
        {
            // If: I have a message that has complex contents, and I get the typed contents of it
            var m = Message.CreateResponse(Common.MessageId, Common.TestMessageContents.DefaultInstance);
            var c = m.GetTypedContents<Common.TestMessageContents>();
            
            // Then: I should get the default instance back
            Assert.Equal(Common.TestMessageContents.DefaultInstance, c);
        }

        [Fact]
        public void GetTypedContentsInvalid()
        {
            // If: I have a message that has contents and I get incorrectly typed contents from it
            // Then: I should get an exception back
            var m = Message.CreateResponse(Common.MessageId, Common.TestMessageContents.DefaultInstance);
            Assert.ThrowsAny<Exception>(() => m.GetTypedContents<int>());
        }
        
        #endregion
    }
}