//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    /// <summary>
    /// Exception thrown when parsing a message from input stream fails.
    /// </summary>
    public class MessageParseException : Exception
    {
        public string OriginalMessageText { get; }

        public MessageParseException(string originalMessageText, string errorMessage, params object[] errorMessageArgs)
            : base(string.Format(errorMessage, errorMessageArgs))
        {
            OriginalMessageText = originalMessageText;
        }
    }

    /// <summary>
    /// Exception thrown when a handler for a given request/event method does not exist
    /// </summary>
    public class MethodHandlerDoesNotExistException : Exception
    {
        public MethodHandlerDoesNotExistException(MessageType type, string method)
            : base(SR.HostingMethodHandlerDoesNotExist(type.ToString(), method))
        {
        }
    }
}
