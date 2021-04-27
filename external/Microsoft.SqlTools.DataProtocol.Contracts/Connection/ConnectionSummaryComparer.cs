//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Connection
{
    
    /// <summary>
    /// Treats connections as the same if their server, db and usernames all match
    /// </summary>
    public class ConnectionSummaryComparer : IEqualityComparer<ConnectionSummary>
    {
        public bool Equals(ConnectionSummary x, ConnectionSummary y)
        {
            if(x == y) { return true; }
            else if(x != null)
            {
                if(y == null) { return false; }

                // Compare server, db, username. Note: server is case-insensitive in the driver
                return string.Compare(x.ServerName, y.ServerName, StringComparison.OrdinalIgnoreCase) == 0
                    && string.Compare(x.DatabaseName, y.DatabaseName, StringComparison.Ordinal) == 0
                    && string.Compare(x.UserName, y.UserName, StringComparison.Ordinal) == 0;
            }
            return false;
        }

        public int GetHashCode(ConnectionSummary obj)
        {
            int hashcode = 31;
            if(obj != null)
            {
                if(obj.ServerName != null)
                {
                    hashcode ^= obj.ServerName.GetHashCode();
                }
                if (obj.DatabaseName != null)
                {
                    hashcode ^= obj.DatabaseName.GetHashCode();
                }
                if (obj.UserName != null)
                {
                    hashcode ^= obj.UserName.GetHashCode();
                }
            }
            return hashcode;
        }
    }
}
