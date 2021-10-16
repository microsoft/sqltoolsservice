//------------------------------------------------------------------------------
// <copyright>
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Runtime.Serialization;

namespace Microsoft.Data.Tools.Contracts.Services
{

    [Serializable]
    internal sealed class InvalidReferenceExceptionEx : Exception
    {
        public InvalidReferenceExceptionEx()
        {
        }

        public InvalidReferenceExceptionEx(string msg) : base(msg)
        {
        }

        private InvalidReferenceExceptionEx(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
