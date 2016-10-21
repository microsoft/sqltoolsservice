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
