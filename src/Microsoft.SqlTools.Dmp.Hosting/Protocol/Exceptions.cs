//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.Dmp.Hosting.Protocol
{
    public class MessageParseException : Exception
    {
        public string OriginalMessageText { get; }

        public MessageParseException(string originalMessageText, string errorMessage, params object[] errorMessageArgs)
            : base(string.Format(errorMessage, errorMessageArgs))
        {
            OriginalMessageText = originalMessageText;
        }
    }

    public class MethodHandlerDoesNotExistException : Exception
    {
        // TODO: Localize
        private const string MessageFormat = "{0} handler for method '{1}' does not exist."; 

        public MethodHandlerDoesNotExistException(MessageType type, string method)
            : base(string.Format(MessageFormat, type, method))
        {
        }
    }
}
