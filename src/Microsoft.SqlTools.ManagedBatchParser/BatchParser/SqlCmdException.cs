//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    public class SqlCmdException : Exception
    {
        public SqlCmdException(string message) : base(message)
        {
        }
    }
}
