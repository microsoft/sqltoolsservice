//------------------------------------------------------------------------------
// <copyright file="SqlModelUpdatingServiceException.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Data.Tools.Schema.Sql.DesignServices
{
    /// <summary>
    /// exception use by SqlModelUpdatingService to indicate
    /// when the scripts can't be updated (such as when a SCC check-out
    /// operation is cancelled)
    /// </summary>
    [Serializable()]
    internal sealed class CannotEditFilesException : Exception
    {
        public CannotEditFilesException(string message)
            : base(message)
        {
        }

        private CannotEditFilesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
