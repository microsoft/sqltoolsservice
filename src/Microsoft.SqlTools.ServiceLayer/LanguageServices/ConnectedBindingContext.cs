//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Class for the binding context for connected sessions
    /// </summary>
    public class ConnectedBindingContext : IBindingContext
    {
        private ParseOptions parseOptions;

        private ManualResetEvent bindingLock;

        private ServerConnection serverConnection;

        private SMO.Server server;

        /// <summary>
        /// Connected binding context constructor
        /// </summary>
        public ConnectedBindingContext()
        {
            this.bindingLock = new ManualResetEvent(initialState: true);            
            this.BindingTimeout = ConnectedBindingQueue.DefaultBindingTimeout;
            this.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
        }

        /// <summary>
        /// Gets or sets a flag indicating if the binder is connected
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the binding server connection
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

                // reset the parse options so the get recreated for the current connection
                this.parseOptions = null;
                this.server = new SMO.Server(this.serverConnection);
            }
        }

        public SMO.Server Server
        {
            get
            {
                // Use the Server from the SmoMetadataProvider if we have it to avoid
                // unnecessary overhead of querying a new object
                return this.SmoMetadataProvider?.SmoServer ?? this.server;
            }
        }

        /// <summary>
        /// Gets or sets the metadata display info provider
        /// </summary>
        public MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }

        /// <summary>
        /// Gets or sets the SMO metadata provider
        /// </summary>
        public SmoMetadataProvider SmoMetadataProvider { get; set; }

        /// <summary>
        /// Gets or sets the binder
        /// </summary>
        public IBinder Binder { get; set; }

        /// <summary>
        /// Gets the binding lock object
        /// </summary>
        public ManualResetEvent BindingLock 
        { 
            get
            {
                return this.bindingLock;
            }
        }

        /// <summary>
        /// Gets or sets the binding operation timeout in milliseconds
        /// </summary>
        public int BindingTimeout { get; set; } 

        /// <summary>
        /// Gets the Language Service ServerVersion
        /// </summary>
        public ServerVersion ServerVersion
        {
            get
            {
                return this.ServerConnection?.ServerVersion;
            }
        }

        /// <summary>
        /// Gets the current DataEngineType
        /// </summary>
        public DatabaseEngineType DatabaseEngineType
        {
            get
            {
                return this.ServerConnection?.DatabaseEngineType ?? DatabaseEngineType.Standalone;
            }
        }

        public DatabaseEngineEdition DatabaseEngineEdition
        {
            get
            {
                return this.ServerConnection?.DatabaseEngineEdition ?? DatabaseEngineEdition.Standard;
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
                    ? GetTransactSqlVersion(this.Server)
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
                    ? GetDatabaseCompatibilityLevel(this.Server)
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
                if (this.parseOptions == null)
                {
                    this.parseOptions = new ParseOptions(
                        batchSeparator: LanguageService.DefaultBatchSeperator,
                        isQuotedIdentifierSet: true, 
                        compatibilityLevel: DatabaseCompatibilityLevel,
                        transactSqlVersion: TransactSqlVersion);
                }
                return this.parseOptions;
            }
        }


        /// <summary>
        /// Gets the database compatibility level for a given server connection
        /// </summary>
        /// <param name="server"></param>
        private static DatabaseCompatibilityLevel GetDatabaseCompatibilityLevel(SMO.Server server)
        {
            if (server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return DatabaseCompatibilityLevel.Azure;
            }

            // Get the actual compat level of the database we're connected to
            switch (server.Databases[server.ConnectionContext.DatabaseName].CompatibilityLevel)
            {
                case SMO.CompatibilityLevel.Version80:
                    return DatabaseCompatibilityLevel.Version80;
                case SMO.CompatibilityLevel.Version90:
                    return DatabaseCompatibilityLevel.Version90;
                case SMO.CompatibilityLevel.Version100:
                    return DatabaseCompatibilityLevel.Version100;
                case SMO.CompatibilityLevel.Version110:
                    return DatabaseCompatibilityLevel.Version110;
                case SMO.CompatibilityLevel.Version120:
                    return DatabaseCompatibilityLevel.Version120;
                case SMO.CompatibilityLevel.Version130:
                    return DatabaseCompatibilityLevel.Version130;
                case SMO.CompatibilityLevel.Version140:
                    return DatabaseCompatibilityLevel.Version140;
                case SMO.CompatibilityLevel.Version150:
                    return DatabaseCompatibilityLevel.Version150;
                default:
                    return DatabaseCompatibilityLevel.Current;
            }
        }

        /// <summary>
        /// Gets the transaction sql version for a given server connection
        /// </summary>
        /// <param name="server"></param>
        private static TransactSqlVersion GetTransactSqlVersion(SMO.Server server)
        {
            if (server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return TransactSqlVersion.Azure;
            }

            // Use the compat level of the database we're connected to for determing what verison
            // of T-SQL to support
            switch (server.Databases[server.ConnectionContext.DatabaseName].CompatibilityLevel)
            {
                case SMO.CompatibilityLevel.Version90:
                case SMO.CompatibilityLevel.Version100:
                    // In case of 10.0 we still use Version 10.5 as it is the closest available.
                    return TransactSqlVersion.Version105;
                case SMO.CompatibilityLevel.Version110:
                    return TransactSqlVersion.Version110;
                case SMO.CompatibilityLevel.Version120:
                    return TransactSqlVersion.Version120;
                case SMO.CompatibilityLevel.Version130:
                    return TransactSqlVersion.Version130;
                case SMO.CompatibilityLevel.Version140:
                    return TransactSqlVersion.Version140;
                case SMO.CompatibilityLevel.Version150:
                    return TransactSqlVersion.Version150;
                default:
                    return TransactSqlVersion.Current;
            }
        }
    }
}
