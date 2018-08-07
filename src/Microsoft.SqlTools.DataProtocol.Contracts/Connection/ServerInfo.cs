//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.Connection
{
    /// <summary>
    /// Contract for information on the connected SQL Server instance.
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// The major version of the SQL Server instance.
        /// </summary>
        public int ServerMajorVersion { get; set; }

        /// <summary>
        /// The minor version of the SQL Server instance.
        /// </summary>
        public int ServerMinorVersion { get; set; }

        /// <summary>
        /// The build of the SQL Server instance.
        /// </summary>
        public int ServerReleaseVersion { get; set; }

        /// <summary>
        /// The ID of the engine edition of the SQL Server instance.
        /// </summary>
        public int EngineEditionId { get; set; }

        /// <summary>
        /// String containing the full server version text.
        /// </summary>
        public string ServerVersion { get; set; }

        /// <summary>
        /// String describing the product level of the server.
        /// </summary>
        public string ServerLevel { get; set; }

        /// <summary>
        /// The edition of the SQL Server instance.
        /// </summary>
        public string ServerEdition { get; set; }

        /// <summary>
        /// Whether the SQL Server instance is running in the cloud (Azure) or not.
        /// </summary>
        public bool IsCloud { get; set; }

        /// <summary>
        /// The version of Azure that the SQL Server instance is running on, if applicable.
        /// </summary>
        public int AzureVersion { get; set; }

        /// <summary>
        /// The Operating System version string of the machine running the SQL Server instance.
        /// </summary>
        public string OsVersion { get; set; }

        /// <summary>
        /// The Operating System version string of the machine running the SQL Server instance.
        /// </summary>
        public string MachineName { get; set; }
    }
}
