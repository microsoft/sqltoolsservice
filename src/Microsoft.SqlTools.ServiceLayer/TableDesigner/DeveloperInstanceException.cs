//------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using Microsoft.Data.Tools.Contracts.Services;

namespace Microsoft.Data.Tools.Schema.Sql.Build
{
    /// <summary>
    /// Thrown by the DeveloperInstanceManager 
    /// </summary>
    [Serializable]
    internal sealed class DeveloperInstanceException : DacContractException
    {
        public DeveloperInstanceException()
        {
        }

        public DeveloperInstanceException(string message)
            : base(message)
        {
        }

        public DeveloperInstanceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        private DeveloperInstanceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
