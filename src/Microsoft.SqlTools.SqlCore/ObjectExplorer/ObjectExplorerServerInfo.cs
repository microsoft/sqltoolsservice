//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer
{
    public class ObjectExplorerServerInfo
    {
        /// <summary>
        /// Server name for the OE session
        /// </summary>
        public string? ServerName { get; set; }
        /// <summary>
        /// Database name for the OE session
        /// </summary>
        public string? DatabaseName { get; set; }
        /// <summary>
        /// User name for the OE session
        /// </summary>
        public string? UserName { get; set; }
        /// <summary>
        /// SQL Server version for the OE session
        /// </summary>
        public string? ServerVersion { get; set; }
        /// <summary>
        /// SQL Server edition for the OE session
        /// </summary>
        public int EngineEditionId { get; set; }
        /// <summary>
        /// Checks if the OE session is for Azure SQL DB
        /// </summary>
        public bool IsCloud { get; set; }
        /// <summary>
        /// Indicates if the OE session is for default or system database
        /// </summary>
        public bool isDefaultOrSystemDatabase { get; set; }
    }
}


    