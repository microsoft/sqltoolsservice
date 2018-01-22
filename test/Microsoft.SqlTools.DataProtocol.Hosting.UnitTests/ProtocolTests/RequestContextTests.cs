//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using Microsoft.SqlTools.DataProtocol.Contracts.Hosting;
using Microsoft.SqlTools.DataProtocol.Hosting.Protocol;
using Xunit;

namespace Microsoft.SqlTools.DataProtocol.Hosting.UnitTests.ProtocolTests
{
    public class RequestContextTests
    {   
        #region Send Tests

        [Fact]
        public void SendResult()
        {
            // Setup: Create a blocking collection to collect the output
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            
            // If: I write a response with the request context
            var rc = new RequestContext<Common.TestMessageContents>(Common.RequestMessage, bc);
            rc.SendResult(Common.TestMessageContents.DefaultInstance);

            // Then: The message writer should have sent a response
            Assert.Equal(1, bc.ToArray().Length);
            Assert.Equal(MessageType.Response, bc.ToArray()[0].MessageType);
        }

        [Fact]
        public void SendEvent()
        {
            // Setup: Create a blocking collection to collect the output
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            
            // If: I write an event with the request context
            var rc = new RequestContext<Common.TestMessageContents>(Common.RequestMessage, bc);
            rc.SendEvent(Common.EventType, Common.TestMessageContents.DefaultInstance);
            
            // Then: The message writer should have sent an event
            Assert.Equal(1, bc.ToArray().Length);
            Assert.Equal(MessageType.Event, bc.ToArray()[0].MessageType);
        }

        [Fact]
        public void SendError()
        {
            // Setup: Create a blocking collection to collect the output
            const string errorMessage = "error";
            const int errorCode = 123;
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            
            // If: I write an error with the request context
            var rc = new RequestContext<Common.TestMessageContents>(Common.RequestMessage, bc);
            rc.SendError(errorMessage, errorCode);
            
            // Then: 
            // ... The message writer should have sent an error
            Assert.Equal(1, bc.ToArray().Length);
            Assert.Equal(MessageType.ResponseError, bc.ToArray()[0].MessageType);
            
            // ... The error object it built should have the reuired fields set
            var contents = bc.ToArray()[0].GetTypedContents<Error>();
            Assert.Equal(errorCode, contents.Code);
            Assert.Equal(errorMessage, contents.Message);
        }

        [Fact]
        public void SendException()
        {
            // Setup: Create a blocking collection to collect the output
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            
            // If: I write an error as an exception with the request context
            const string errorMessage = "error";
            var e = new Exception(errorMessage);
            var rc = new RequestContext<Common.TestMessageContents>(Common.RequestMessage, bc);
            rc.SendError(e);
            
            // Then: 
            // ... The message writer should have sent an error
            Assert.Equal(1, bc.ToArray().Length);
            Assert.Equal(MessageType.ResponseError, bc.ToArray()[0].MessageType);
            
            // ... The error object it built should have the reuired fields set
            var contents = bc.ToArray()[0].GetTypedContents<Error>();
            Assert.Equal(e.HResult, contents.Code);
            Assert.Equal(errorMessage, contents.Message);
            
        }
        
        #endregion
    }
}