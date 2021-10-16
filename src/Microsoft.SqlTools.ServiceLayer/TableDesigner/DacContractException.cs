//------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Runtime.Serialization;

namespace Microsoft.Data.Tools.Contracts.Services
{
    /// <summary>
    /// Represents a base class for all design time exceptions
    /// </summary>
    [Serializable]
    internal abstract class DacContractException : Exception
    {
        protected DacContractException()
        {
        }

        protected DacContractException(string message)
            : base(message)
        {
        }

        protected DacContractException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DacContractException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
