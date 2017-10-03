//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// Includes the data for a discovered database
    /// </summary>
    public class DatabaseInstanceInfo
    {
        /// <summary>
        /// Default constructor to initialize the instance 
        /// </summary>
        /// <param name="serverInstanceInfo"></param>
        public DatabaseInstanceInfo(ServerInstanceInfo serverInstanceInfo)
        {
            ServerInstanceInfo = serverInstanceInfo;
        }

        public DatabaseInstanceInfo(ServerDefinition serverDefinition, string serverName, string databaseName)
        {
            ServerInstanceInfo = new ServerInstanceInfo(serverDefinition)
            {
                Name = serverName
            };
            Name = databaseName;
        }

        /// <summary>
        /// Server instance info associated to the database instance
        /// </summary>
        public ServerInstanceInfo ServerInstanceInfo
        {
            get; 
            private set;
        }

        /// <summary>
        /// Database Name
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Returns true if the database is the master database
        /// </summary>
        public bool IsMaster
        {
            get { return ConnectionConstants.MasterDatabaseName.Equals(Name, StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsDefaultDatabase { get; set; }

        public bool IsSystemDatabase { get; set; }
    }
}
