//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Utility;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public class MessageWriter
    {
        private const string ContentLength = "Content-Length: ";
        private const string ContentType = "Content-Type: application/json";
        private const string HeaderSeparator = "\r\n";
        private const string HeaderEnd = "\r\n\r\n";

        private readonly Stream outputStream;
        private readonly AsyncLock writeLock = new AsyncLock();

        public MessageWriter(Stream outputStream)
        {
            Validate.IsNotNull("streamWriter", outputStream);

            this.outputStream = outputStream;
        }
        
        public virtual async Task WriteMessage(Message messageToWrite)
        {
            Validate.IsNotNull("messageToWrite", messageToWrite);

            // Log the JSON representation of the message
            string logMessage = string.Format("Sending message of type[{0}] and method[{1}]",
                messageToWrite.MessageType, messageToWrite.Method);
            Logger.Instance.Write(LogLevel.Verbose, logMessage);

            string serializedMessage = messageToWrite.Serialize();
            // TODO: Allow encoding to be passed in
            byte[] messageBytes = Encoding.UTF8.GetBytes(serializedMessage);

            string headers = ContentLength + messageBytes.Length + HeaderSeparator
                             + ContentType + HeaderEnd;
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);

            // Make sure only one call is writing at a time.  You might be thinking
            // "Why not use a normal lock?"  We use an AsyncLock here so that the
            // message loop doesn't get blocked while waiting for I/O to complete.
            using (await writeLock.LockAsync())
            {
                // Send the message
                await outputStream.WriteAsync(headerBytes, 0, headerBytes.Length);
                await outputStream.WriteAsync(messageBytes, 0, messageBytes.Length);
                await outputStream.FlushAsync();
            }
        }
    }
}
