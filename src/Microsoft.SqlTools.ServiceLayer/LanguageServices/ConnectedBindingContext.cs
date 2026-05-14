//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Class for the binding context for connected sessions
    /// </summary>
    public class ConnectedBindingContext : IBindingContext
    {
        private const int CompatibilityProbeTimeoutSeconds = 5;

        private ParseOptions parseOptions;

        private ManualResetEvent bindingLock;

        private ServerConnection serverConnection;

        private SMO.Server server;

        private DatabaseCompatibilityLevel? cachedDatabaseCompatibilityLevel;

        private TransactSqlVersion? cachedTransactSqlVersion;

        private DateTime? noOpBinderExpiresAtUtc;

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
        /// Gets or sets a flag indicating if the binder has a live server connection (SMO).
        /// False for project-based offline contexts even though they are fully ready to bind.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating this is an offline project-based binding context.
        /// When true, <see cref="IsConnected"/> is false, <see cref="ServerConnection"/> is null,
        /// and <see cref="ParseOptions"/> is served from <see cref="ProjectParseOptions"/>.
        /// </summary>
        public bool IsProjectContext { get; set; }

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
                this.cachedDatabaseCompatibilityLevel = null;
                this.cachedTransactSqlVersion = null;
                // Set up a SMO Server to query when determing parse options and we don't have a metadataprovider 
                this.server = this.serverConnection == null ? null : new SMO.Server(this.serverConnection);
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
        /// Gets or sets the metadata provider (for project-based IntelliSense)
        /// </summary>
        public IMetadataProvider MetadataProvider { get; set; }

        /// <summary>
        /// Gets or sets the binder
        /// </summary>
        public IBinder Binder { get; set; }

        internal bool IsDead => this.noOpBinderExpiresAtUtc.HasValue &&
                                DateTime.UtcNow >= this.noOpBinderExpiresAtUtc.Value;

        internal void UseNoOpBinder(TimeSpan lifetime)
        {
            this.Binder = NoOpBinder.Instance;
            this.IsConnected = true;
            this.noOpBinderExpiresAtUtc = DateTime.UtcNow.Add(lifetime);
        }

        internal void MarkConnected()
        {
            this.IsConnected = true;
            this.noOpBinderExpiresAtUtc = null;
        }

        /// <summary>
        /// Parse options for project-based offline binding contexts.
        /// Set at context creation time; only read when <see cref="IsProjectContext"/> is true.
        /// </summary>
        public ParseOptions ProjectParseOptions { get; set; }

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
                EnsureServerVersionInfo();
                return this.cachedTransactSqlVersion ?? TransactSqlVersion.Current;
            }
        }

        /// <summary>
        /// Gets the current DatabaseCompatibilityLevel
        /// </summary>
        public DatabaseCompatibilityLevel DatabaseCompatibilityLevel
        {
            get
            {
                EnsureServerVersionInfo();
                return this.cachedDatabaseCompatibilityLevel ?? DatabaseCompatibilityLevel.Current;
            }
        }

        /// <summary>
        /// Gets the current ParseOptions. For project contexts returns the options supplied at
        /// context creation time; for connected contexts derives options from the server connection.
        /// </summary>
        public ParseOptions ParseOptions
        {
            get
            {
                if (this.IsProjectContext)
                {
                    return this.ProjectParseOptions;
                }
                this.parseOptions ??= new ParseOptions(
                        batchSeparator: LanguageService.DefaultBatchSeperator,
                        isQuotedIdentifierSet: true, 
                        compatibilityLevel: DatabaseCompatibilityLevel,
                        transactSqlVersion: TransactSqlVersion);
                return this.parseOptions;
            }
        }

        private void EnsureServerVersionInfo()
        {
            if (this.cachedDatabaseCompatibilityLevel.HasValue && this.cachedTransactSqlVersion.HasValue)
            {
                return;
            }

            if (!this.IsConnected || this.IsProjectContext || this.Server == null)
            {
                this.cachedDatabaseCompatibilityLevel = DatabaseCompatibilityLevel.Current;
                this.cachedTransactSqlVersion = TransactSqlVersion.Current;
                return;
            }

            try
            {
                if (this.Server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    this.cachedDatabaseCompatibilityLevel = DatabaseCompatibilityLevel.Azure;
                    this.cachedTransactSqlVersion = TransactSqlVersion.Azure;
                    return;
                }

                SMO.CompatibilityLevel compatibilityLevel = GetServerCompatibilityLevel(this.Server);
                this.cachedDatabaseCompatibilityLevel = ToDatabaseCompatibilityLevel(compatibilityLevel);
                this.cachedTransactSqlVersion = ToTransactSqlVersion(this.Server.VersionMajor, compatibilityLevel);
            }
            catch (Exception ex)
            {
                Logger.Information($"Failed to initialize binding parse options - using current defaults. Exception: {ex}");
                this.cachedDatabaseCompatibilityLevel = DatabaseCompatibilityLevel.Current;
                this.cachedTransactSqlVersion = TransactSqlVersion.Current;
            }
        }


        /// <summary>
        /// Gets the database compatibility level for a given server connection
        /// </summary>
        /// <param name="server"></param>
        private static DatabaseCompatibilityLevel ToDatabaseCompatibilityLevel(SMO.CompatibilityLevel compatibilityLevel)
        {
            switch (compatibilityLevel)
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
        private static TransactSqlVersion ToTransactSqlVersion(int serverVersionMajor, SMO.CompatibilityLevel compatibilityLevel)
        {
            // Determine the language version to use - we can't just use VersionMajor directly because there are engine versions (such as MI)
            // whose language version they support is higher than the actual server version. So we choose the highest compat level from
            // between the server version and compat level
            var compatLevel = Math.Max(serverVersionMajor * 10, (int)compatibilityLevel);
            switch (compatLevel)
            {
                case 90:
                case 100:
                    // In case of 10.0 we still use Version 10.5 as it is the closest available.
                    return TransactSqlVersion.Version105;
                case 110:
                    return TransactSqlVersion.Version110;
                case 120:
                    return TransactSqlVersion.Version120;
                case 130:
                    return TransactSqlVersion.Version130;
                case 140:
                    return TransactSqlVersion.Version140;
                case 150:
                    return TransactSqlVersion.Version150;
                default:
                    return TransactSqlVersion.Current;
            }
        }

        /// <summary>
        /// Gets the SMO compatibility level for the given server, defaulting to the highest available level if an
        /// error occurs while querying. 
        /// </summary>
        /// <param name="server">The server object to get the compat level of</param>
        /// <returns></returns>
        private static SMO.CompatibilityLevel GetServerCompatibilityLevel(SMO.Server server)
        {
            // Set the default fields so that we avoid the overhead of querying for properties we don't need right now
            server.SetDefaultInitFields(typeof(SMO.Database), nameof(SMO.Database.CompatibilityLevel));

            SMO.CompatibilityLevel compatLevel;
            int originalStatementTimeout = server.ConnectionContext.StatementTimeout;
            try
            {
                server.ConnectionContext.StatementTimeout = originalStatementTimeout <= 0
                    ? CompatibilityProbeTimeoutSeconds
                    : Math.Min(originalStatementTimeout, CompatibilityProbeTimeoutSeconds);
                // First try the master DB since it will have the highest compat level for that instance
                compatLevel = server.Databases["master"].CompatibilityLevel;
                Logger.Information($"Got compat level for binding context {compatLevel} after querying master");
            }
            catch
            {
                // If we can't get it from master then fall back to the current database
                try
                {
                    compatLevel = server.Databases[server.ConnectionContext.DatabaseName].CompatibilityLevel;
                    Logger.Information($"Got compat level for binding context {compatLevel} after querying connection DB");
                }
                catch
                {
                    // There's nothing else we can do so just default to the highest available version
                    compatLevel = Enum.GetValues<SMO.CompatibilityLevel>().Cast<SMO.CompatibilityLevel>().Max();
                    Logger.Information($"Failed to get compat level for binding context from querying server - using default of {compatLevel}");
                }

            }
            finally
            {
                server.ConnectionContext.StatementTimeout = originalStatementTimeout;
            }
            return compatLevel;
        }
    }
}
