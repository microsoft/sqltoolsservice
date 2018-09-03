//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.SqlTools.DataProtocol.Contracts.Connection
{

    public interface IConnectionSummary
    {
        /// <summary>
        /// Gets or sets the connection server name
        /// </summary>
       string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the connection database name
        /// </summary>
        string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the connection user name
        /// </summary>
        string UserName { get; set; }
    }

    /// <summary>
    /// Provides high level information about a connection.
    /// </summary>
    public class ConnectionSummary : IConnectionSummary
    {
        /// <summary>
        /// Gets or sets the connection server name
        /// </summary>
        public virtual string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the connection database name
        /// </summary>
        public virtual string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the connection user name
        /// </summary>
        public virtual string UserName { get; set; }
    }
}
