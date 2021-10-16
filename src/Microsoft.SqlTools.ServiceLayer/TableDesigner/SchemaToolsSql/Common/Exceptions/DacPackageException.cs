//------------------------------------------------------------------------------
// <copyright>
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions
{
    /// <summary>
    /// Thrown when something went wrong when instantiating/loading extensions
    /// </summary>
    [Serializable]
    internal sealed class DacPackageException : Exception
	{
        public DacPackageException()
            : this(null, null)
        {
        }

        public DacPackageException(String message)
            : this(message, null)
        {
        }

        public DacPackageException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private DacPackageException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

        public DacPackageException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }
	}
}

