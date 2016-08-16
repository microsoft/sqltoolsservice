//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Information pertaining to a unique connection instance.
    /// </summary>
    public class ConnectionInfo
    {
        public ConnectionInfo(ISqlConnectionFactory factory, string ownerUri, ConnectionDetails details)
        {
            Factory = factory;
            OwnerUri = ownerUri;
            ConnectionDetails = details;
            ConnectionId = Guid.NewGuid();
        }

        /// <summary>
        /// Unique Id, helpful to identify a connection info object
        /// </summary>
        public Guid ConnectionId { get; private set; }

        public string OwnerUri { get; private set; }

        public ISqlConnectionFactory Factory {get; private set;}

        public ConnectionDetails ConnectionDetails { get; private set; }
        
        public DbConnection SqlConnection { get; set; }
    }
}
