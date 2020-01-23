//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    /// <summary>
    /// Specific exception type for SQLCMD related issues
    /// </summary>
    public class SqlCmdException : Exception
    {
        public SqlCmdException(string message) : base(message)
        {
        }
    }
}
