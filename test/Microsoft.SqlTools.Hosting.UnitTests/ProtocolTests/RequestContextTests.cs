//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.SqlTools.Hosting.Contracts.Internal;
using Microsoft.SqlTools.Hosting.Protocol;
using NUnit.Framework;

namespace Microsoft.SqlTools.Hosting.UnitTests.ProtocolTests
{
    [TestFixture]
    public class RequestContextTests
    {   
        #region Send Tests

        [Test]
        public void SendResult()
        {
            // Setup: Create a blocking collection to collect the output
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            
            // If: I write a response with the request context
            var rc = new RequestContext<CommonObjects.TestMessageContents>(CommonObjects.RequestMessage, bc);
            rc.SendResult(CommonObjects.TestMessageContents.DefaultInstance);

            Assert.That(bc.Select(m => m.MessageType), Is.EqualTo(new[] { MessageType.Response }), "The message writer should have sent a response");
        }

        [Test]
        public void SendEvent()
        {
            // Setup: Create a blocking collection to collect the output
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            
            // If: I write an event with the request context
            var rc = new RequestContext<CommonObjects.TestMessageContents>(CommonObjects.RequestMessage, bc);
            rc.SendEvent(CommonObjects.EventType, CommonObjects.TestMessageContents.DefaultInstance);

            Assert.That(bc.Select(m => m.MessageType), Is.EqualTo(new[] { MessageType.Event }), "The message writer should have sent an event");
        }

        [Test]
        public void SendError()
        {
            // Setup: Create a blocking collection to collect the output
            const string errorMessage = "error";
            const int errorCode = 123;
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            
            // If: I write an error with the request context
            var rc = new RequestContext<CommonObjects.TestMessageContents>(CommonObjects.RequestMessage, bc);
            rc.SendError(errorMessage, errorCode);
            
            Assert.That(bc.Select(m => m.MessageType), Is.EqualTo(new[] { MessageType.ResponseError }), "The message writer should have sent an error");

            // ... The error object it built should have the reuired fields set
            var contents = bc.ToArray()[0].GetTypedContents<Error>();
            Assert.AreEqual(errorCode, contents.Code);
            Assert.AreEqual(errorMessage, contents.Message);
        }

        [Test]
        public void SendException()
        {
            // Setup: Create a blocking collection to collect the output
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            
            // If: I write an error as an exception with the request context
            const string errorMessage = "error";
            var e = new Exception(errorMessage);
            var rc = new RequestContext<CommonObjects.TestMessageContents>(CommonObjects.RequestMessage, bc);
            rc.SendError(e);

            Assert.That(bc.Select(m => m.MessageType), Is.EqualTo(new[] { MessageType.ResponseError }), "The message writer should have sent an error");

            // ... The error object it built should have the reuired fields set
            var contents = bc.First().GetTypedContents<Error>();
            Assert.AreEqual(e.HResult, contents.Code);
            Assert.AreEqual(errorMessage, contents.Message);
            
        }
        
        #endregion
    }
}