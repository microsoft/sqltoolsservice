//------------------------------------------------------------------------------
// <copyright file="ErrorSeverity.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    internal enum ErrorSeverity
    {
        Unknown = 0,
        Error,
        Warning,
        Message
    }
}
