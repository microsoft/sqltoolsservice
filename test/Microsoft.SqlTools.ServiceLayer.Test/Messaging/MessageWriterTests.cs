//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Serializers;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Messaging
{
    public class MessageWriterTests
    {
        private readonly IMessageSerializer messageSerializer;

        public MessageWriterTests()
        {
            this.messageSerializer = new V8MessageSerializer();
        }

        [Fact]
        public async Task WritesMessage()
        {
            MemoryStream outputStream = new MemoryStream();
            MessageWriter messageWriter = new MessageWriter(outputStream, this.messageSerializer);

            // Write the message and then roll back the stream to be read
            // TODO: This will need to be redone!
            await messageWriter.WriteMessage(Hosting.Protocol.Contracts.Message.Event("testEvent", null));
            outputStream.Seek(0, SeekOrigin.Begin);

            string expectedHeaderString = string.Format(Constants.ContentLengthFormatString,
                Common.ExpectedMessageByteCount);

            byte[] buffer = new byte[128];
            await outputStream.ReadAsync(buffer, 0, expectedHeaderString.Length);

            Assert.Equal(
                expectedHeaderString,
                Encoding.ASCII.GetString(buffer, 0, expectedHeaderString.Length));

            // Read the message
            await outputStream.ReadAsync(buffer, 0, Common.ExpectedMessageByteCount);

            Assert.Equal(Common.TestEventString, 
                Encoding.UTF8.GetString(buffer, 0, Common.ExpectedMessageByteCount));

            outputStream.Dispose();
        }

    }
}
