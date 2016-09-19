//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Class for storing cached metadata regarding a parsed SQL file
    /// </summary>
    internal class ScriptParseInfo
    {
        private ManualResetEvent buildingMetadataEvent = new ManualResetEvent(initialState: true);

        private ParseOptions parseOptions = new ParseOptions();

        private ServerConnection serverConnection;

        /// <summary>
        /// Event which tells if MetadataProvider is built fully or not
        /// </summary>
        public ManualResetEvent BuildingMetadataEvent 
        { 
            get { return this.buildingMetadataEvent; }
        }

        /// <summary>
        /// Gets or sets a flag determining is the LanguageService is connected
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the LanguageService SMO ServerConnection
        /// </summary>
        public ServerConnection ServerConnection 
        { 
            get 
            { 
                return this.serverConnection; 
            } 
            set
            {
                this.serverConnection = value;
                this.parseOptions = new ParseOptions(
                    batchSeparator: LanguageService.DefaultBatchSeperator,
                    isQuotedIdentifierSet: true, 
                    compatibilityLevel: DatabaseCompatibilityLevel, 
                    transactSqlVersion: TransactSqlVersion);
            }
        }

        /// <summary>
        /// Gets the Language Service ServerVersion
        /// </summary>
        public ServerVersion ServerVersion 
        { 
            get 
            { 
                return this.ServerConnection != null
                    ? this.ServerConnection.ServerVersion
                    : null; 
            }
        }

        /// <summary>
        /// Gets the current DataEngineType
        /// </summary>
        public DatabaseEngineType DatabaseEngineType 
        { 
            get 
            { 
                return this.ServerConnection != null
                    ? this.ServerConnection.DatabaseEngineType
                    : DatabaseEngineType.Standalone; 
            }
        }

        /// <summary>
        /// Gets the current connections TransactSqlVersion
        /// </summary>
        public TransactSqlVersion TransactSqlVersion
        {
            get
            {
                return this.IsConnected 
                    ? GetTransactSqlVersion(this.ServerVersion)
                    : TransactSqlVersion.Current;
            }
        }
          
        /// <summary>
        /// Gets the current DatabaseCompatibilityLevel
        /// </summary>
        public DatabaseCompatibilityLevel DatabaseCompatibilityLevel
        {
            get
            {
                return this.IsConnected
                    ? GetDatabaseCompatibilityLevel(this.ServerVersion)
                    : DatabaseCompatibilityLevel.Current;
            }
        }

        /// <summary>
        /// Gets the current ParseOptions
        /// </summary>
        public ParseOptions ParseOptions
        {
            get
            {
                return this.parseOptions;
            }
        }

        /// <summary>
        /// Gets or sets the SMO binder for schema-aware intellisense
        /// </summary>
        public IBinder Binder { get; set; }

        /// <summary>
        /// Gets or sets the previous SQL parse result
        /// </summary>
        public ParseResult ParseResult { get; set; }

        /// <summary>
        /// Gets or set the SMO metadata provider that's bound to the current connection
        /// </summary>
        public SmoMetadataProvider MetadataProvider { get; set; }

        /// <summary>
        /// Gets or sets the SMO metadata display info provider
        /// </summary>
        public MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }
        
        /// <summary>
        /// Gets or sets the current autocomplete suggestion list
        /// </summary>
        public IEnumerable<Declaration> CurrentSuggestions { get; set; }

        /// <summary>
        /// Gets the database compatibility level from a server version
        /// </summary>
        /// <param name="serverVersion"></param>
        private static DatabaseCompatibilityLevel GetDatabaseCompatibilityLevel(ServerVersion serverVersion)
        {
            int versionMajor = Math.Max(serverVersion.Major, 8);

            switch (versionMajor)
            {
                case 8:
                    return DatabaseCompatibilityLevel.Version80;
                case 9:
                    return DatabaseCompatibilityLevel.Version90;
                case 10:
                    return DatabaseCompatibilityLevel.Version100;
                case 11:
                    return DatabaseCompatibilityLevel.Version110;
                case 12:
                    return DatabaseCompatibilityLevel.Version120;
                case 13:
                    return DatabaseCompatibilityLevel.Version130;
                default:
                    return DatabaseCompatibilityLevel.Current;
            }
        }

        /// <summary>
        /// Gets the transaction sql version from a server version
        /// </summary>
        /// <param name="serverVersion"></param>
        private static TransactSqlVersion GetTransactSqlVersion(ServerVersion serverVersion)
        {
            int versionMajor = Math.Max(serverVersion.Major, 9);

            switch (versionMajor)
            {
                case 9:
                case 10:
                    // In case of 10.0 we still use Version 10.5 as it is the closest available.
                    return TransactSqlVersion.Version105;
                case 11:
                    return TransactSqlVersion.Version110;
                case 12:
                    return TransactSqlVersion.Version120;
                case 13:
                    return TransactSqlVersion.Version130;
                default:
                    return TransactSqlVersion.Current;
            }
        }
    }
}
