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
    internal sealed class SqlExtensibilityException : Exception
	{
        public SqlExtensibilityException()
            : this(null, null)
        {
        }

        public SqlExtensibilityException(String message)
            : this(message, null)
        {
        }

        public SqlExtensibilityException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SqlExtensibilityException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}

