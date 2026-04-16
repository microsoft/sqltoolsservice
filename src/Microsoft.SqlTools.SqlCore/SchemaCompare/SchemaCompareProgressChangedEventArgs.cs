//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Event args for schema compare publish progress updates.
    /// </summary>
    public class SchemaCompareProgressChangedEventArgs : EventArgs
    {
        public SchemaCompareProgressChangedEventArgs(string status, object rawEventArgs)
        {
            Status = status;
            RawEventArgs = rawEventArgs;
        }

        public string Status { get; }

        public object RawEventArgs { get; }
    }
}
