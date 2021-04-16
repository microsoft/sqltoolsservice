//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.SqlTools.ResourceProvider.Core
{
    /// <summary>
    /// The exception is used if any operation fails as a request failed due to an expired token 
    /// </summary>
    public class ExpiredTokenException : ServiceExceptionBase
    {
        /// <summary>
        /// Initializes a new instance of the ServiceFailedException class.
        /// </summary>
        public ExpiredTokenException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ServiceFailedException class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        public ExpiredTokenException(string message)
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
        public ExpiredTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ServiceFailedException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        public ExpiredTokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
