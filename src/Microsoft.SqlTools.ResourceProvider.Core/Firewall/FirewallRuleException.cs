﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.SqlTools.ResourceProvider.Core.Firewall
{
    /// <summary>
    /// Exception used by firewall service to indicate when firewall rule operation fails
    /// </summary>
    public class FirewallRuleException : ServiceExceptionBase
    {
        /// <summary>
        /// Initializes a new instance of the AuthenticationFailedException class.
        /// </summary>
        public FirewallRuleException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationFailedException class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        public FirewallRuleException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationFailedException class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        /// <param name="httpStatusCode">The Http error code. </param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        /// (Nothing in Visual Basic) if no inner exception is specified</param>
        public FirewallRuleException(string message, HttpStatusCode httpStatusCode, Exception innerException = null)
            : base(message, httpStatusCode, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationFailedException class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        /// <param name="httpStatusCode">The Http error code. </param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        /// (Nothing in Visual Basic) if no inner exception is specified</param>
        public FirewallRuleException(string message, int httpStatusCode, Exception innerException = null)
            : base(message, httpStatusCode, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationFailedException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        /// (Nothing in Visual Basic) if no inner exception is specified</param>
        public FirewallRuleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationFailedException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        public FirewallRuleException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}