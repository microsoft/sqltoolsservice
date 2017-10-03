//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// List of built-in server types used in <see cref="ExportableAttribute" />. 
    /// Defines a server grouping based on the type of server connection supported (SQL Server, Reporting Server, Analysis Server)
    /// Additional server types can be defined as needed.
    /// Note that the Connection Dialog UI may require server type to be set for some resource types such as<see cref="IServerDiscoveryProvider" />. 
    /// In addition a UI section matching that category may be required, or else the provider will not be used by any UI part and never be called.
    /// </summary>
    public static class ServerTypes
    {
        /// <summary>
        /// Sql server
        /// </summary>
        public const string SqlServer = "SqlServer";

        /// <summary>
        /// Reporting server
        /// </summary>
        public const string SqlReportingServer = "SqlReportingServer";

        /// <summary>
        /// Integration server
        /// </summary>
        public const string SqlIntegrationServer = "SqlIntegrationServer";

        /// <summary>
        /// Analysis server
        /// </summary>
        public const string SqlAnalysisServer = "SqlAnalysisServer";
    }

    /// <summary>
    /// List of built-in categories used in <see cref="ExportableAttribute" />
    /// Defines a server grouping based on the category of server connection supported (Network, Local, Azure)
    /// Additional categories can be defined as needed.
    /// Note that the Connection Dialog UI may require Category to be set for some resource types such as<see cref="IServerDiscoveryProvider" />. 
    /// In addition a UI section matching that category may be required, or else the provider will not be used by any UI part and never be called.
    /// </summary>
    public static class Categories
    {
        /// <summary>
        /// Network category
        /// </summary>
        public const string Network = "network";

        /// <summary>
        /// Azure category
        /// </summary>
        public const string Azure = "azure";

        /// <summary>
        /// local category
        /// </summary>
        public const string Local = "local";

        /// <summary>
        /// local db category
        /// </summary>
        public const string LocalDb = "localdb";
    }
}
