//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.Hosting.Protocol;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Microsoft.SqlTools.Hosting.UnitTests.ProtocolTests
{
    [TestFixture]
    public class MessageTests
    {
        #region Construction/Serialization Tests
        
        [Test]
        public void CreateRequest()
        {
            // If: I create a request
            var message = Message.CreateRequest(CommonObjects.RequestType, CommonObjects.MessageId, CommonObjects.TestMessageContents.DefaultInstance);
            
            // Then:
            // ... The message should have all the properties I defined
            // ... The JObject should have the same properties
            var expectedResults = new MessagePropertyResults
            {
                MessageType = MessageType.Request,
                IdSet = true,
                MethodSetAs = CommonObjects.RequestType.MethodName,
                ContentsSetAs = "params",
                ErrorSet = false
            };
            AssertPropertiesSet(expectedResults, message);
        }

        [Test]
        public void CreateError()
        {
            // If: I create an error
            var message = Message.CreateResponseError(CommonObjects.MessageId, CommonObjects.TestErrorContents.DefaultInstance);
            
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

        [Test]
        public void CreateResponse()
        {
            // If: I create a response
            var message = Message.CreateResponse(CommonObjects.MessageId, CommonObjects.TestMessageContents.DefaultInstance);
            
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

        [Test]
        public void CreateEvent()
        {
            // If: I create an event
            var message = Message.CreateEvent(CommonObjects.EventType, CommonObjects.TestMessageContents.DefaultInstance);
            
            // Then: Message and JObject should have appropriate properties set
            var expectedResults = new MessagePropertyResults
            {
                MessageType = MessageType.Event,
                IdSet = false,
                MethodSetAs = CommonObjects.EventType.MethodName,
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
            Assert.AreEqual("2.0", jObject["jsonrpc"].Value<string>());
            
            // Message Type
            Assert.AreEqual(results.MessageType, message.MessageType);
            
            // ID
            if (results.IdSet)
            {
                Assert.AreEqual(CommonObjects.MessageId, message.Id);
                Assert.AreEqual(CommonObjects.MessageId, jObject["id"].Value<string>());
                expectedProperties.Add("id");
            }
            else
            {
                Assert.Null(message.Id);
            }

            // Method
            if (results.MethodSetAs != null)
            {
                Assert.AreEqual(results.MethodSetAs, message.Method);
                Assert.AreEqual(results.MethodSetAs, jObject["method"].Value<string>());
                expectedProperties.Add("method");
            }
            else
            {
                Assert.Null(message.Method);
            }

            // Contents
            if (results.ContentsSetAs != null)
            {
                Assert.AreEqual(CommonObjects.TestMessageContents.SerializedContents, message.Contents);
                Assert.AreEqual(CommonObjects.TestMessageContents.SerializedContents, jObject[results.ContentsSetAs]);
                expectedProperties.Add(results.ContentsSetAs);
            }
            
            // Error
            if (results.ErrorSet)
            {
                Assert.AreEqual(CommonObjects.TestErrorContents.SerializedContents, message.Contents);
                Assert.AreEqual(CommonObjects.TestErrorContents.SerializedContents, jObject["error"]);
                expectedProperties.Add("error");
            }
            
            // Look for any extra properties set in the JObject
            IEnumerable<string> setProperties = jObject.Properties().Select(p => p.Name);
            Assert.That(setProperties.Except(expectedProperties), Is.Empty, "extra properties in jObject");
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

        [Test]
        public void DeserializeMissingJsonRpc()
        {
            // If: I deserialize a json string that doesn't have a JSON RPC version
            // Then: I should get an exception
            Assert.Throws<MessageParseException>(() => Message.Deserialize("{\"id\": 123}"));
        }
        
        [Test]
        public void DeserializeEvent()
        {
            // If: I deserialize an event json string
            Message m = Message.Deserialize("{\"jsonrpc\": \"2.0\", \"params\": {}, \"method\": \"event\"}");
            
            // Then: I should get an event message back
            Assert.NotNull(m);
            Assert.AreEqual(MessageType.Event, m.MessageType);
            Assert.AreEqual("event", m.Method);
            Assert.NotNull(m.Contents);
            Assert.Null(m.Id);
        }

        [Test]
        public void DeserializeEventMissingMethod()
        {
            // If: I deserialize an event json string that is missing a method
            // Then: I should get an exception
            Assert.Throws<MessageParseException>(() => Message.Deserialize("{\"jsonrpc\": \"2.0\", \"params\": {}}")); 
        }

        [Test]
        public void DeserializeResponse()
        {
            // If: I deserialize a response json string
            Message m = Message.Deserialize("{\"jsonrpc\": \"2.0\", \"result\": {}, \"id\": \"123\"}");
            
            // Then: I should get a response message back
            Assert.NotNull(m);
            Assert.AreEqual(MessageType.Response, m.MessageType);
            Assert.AreEqual("123", m.Id);
            Assert.NotNull(m.Contents);
            Assert.Null(m.Method);
        }

        [Test]
        public void DeserializeErrorResponse()
        {
            // If: I deserialize an error response
            Message m = Message.Deserialize("{\"jsonrpc\": \"2.0\", \"error\": {}, \"id\": \"123\"}");
            
            // Then: I should get an error response message back
            Assert.NotNull(m);
            Assert.AreEqual(MessageType.ResponseError, m.MessageType);
            Assert.AreEqual("123", m.Id);
            Assert.NotNull(m.Contents);
            Assert.Null(m.Method);
        }

        [Test]
        public void DeserializeRequest()
        {
            // If: I deserialize a request
            Message m = Message.Deserialize("{\"jsonrpc\": \"2.0\", \"params\": {}, \"method\": \"request\", \"id\": \"123\"}");
            
            // Then: I should get a request message back
            Assert.NotNull(m);
            Assert.AreEqual(MessageType.Request, m.MessageType);
            Assert.AreEqual("123", m.Id);
            Assert.AreEqual("request", m.Method);
            Assert.NotNull(m.Contents);
        }

        [Test]
        public void DeserializeRequestMissingMethod()
        {
            // If: I deserialize a request that doesn't have a method parameter
            // Then: I should get an exception
            Assert.Throws<MessageParseException>(() => Message.Deserialize("{\"jsonrpc\": \"2.0\", \"params\": {}, \"id\": \"123\"}"));
        }

        [Test]
        public void GetTypedContentsNull()
        {
            // If: I have a message that has a null contents, and I get the typed contents of it
            var m = Message.CreateResponse<CommonObjects.TestMessageContents>(CommonObjects.MessageId, null);
            var c = m.GetTypedContents<CommonObjects.TestMessageContents>();
            
            // Then: I should get null back as the test message contents
            Assert.Null(c);
        }

        [Test]
        public void GetTypedContentsSimpleValue()
        {
            // If: I have a message that has simple contents, and I get the typed contents of it
            var m = Message.CreateResponse(CommonObjects.MessageId, 123);
            var c = m.GetTypedContents<int>();
            
            // Then: I should get an int back
            Assert.AreEqual(123, c);
        }

        [Test]
        public void GetTypedContentsClassValue()
        {
            // If: I have a message that has complex contents, and I get the typed contents of it
            var m = Message.CreateResponse(CommonObjects.MessageId, CommonObjects.TestMessageContents.DefaultInstance);
            var c = m.GetTypedContents<CommonObjects.TestMessageContents>();
            
            // Then: I should get the default instance back
            Assert.AreEqual(CommonObjects.TestMessageContents.DefaultInstance, c);
        }

        [Test]
        public void GetTypedContentsInvalid()
        {
            // If: I have a message that has contents and I get incorrectly typed contents from it
            // Then: I should get an exception back
            var m = Message.CreateResponse(CommonObjects.MessageId, CommonObjects.TestMessageContents.DefaultInstance);
            Assert.Throws<ArgumentException>(() => m.GetTypedContents<int>());
        }
        
        #endregion
    }
}