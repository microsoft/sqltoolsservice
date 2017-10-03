//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// The exception is used if any operation fails becauase user needs to reauthenticate 
    /// </summary>
    public class UserNeedsAuthenticationException : ServiceExceptionBase
    {
        /// <summary>
        /// Initializes a new instance of the ServiceFailedException class.
        /// </summary>
        public UserNeedsAuthenticationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ServiceFailedException class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        public UserNeedsAuthenticationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ServiceFailedException class with a specified error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference 
        /// (Nothing in Visual Basic) if no inner exception is specified</param>
        public UserNeedsAuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ServiceFailedException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        public UserNeedsAuthenticationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
