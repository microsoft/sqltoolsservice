//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// Event arguments to use for database info events
    /// </summary>
    public class DatabaseInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Database Info
        /// </summary>
        public DatabaseInstanceInfo Database
        {
            get; set;
        }
    }
}
