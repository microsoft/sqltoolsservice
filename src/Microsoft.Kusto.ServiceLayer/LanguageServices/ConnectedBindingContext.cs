//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.Kusto.ServiceLayer.DataSource;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Class for the binding context for connected sessions
    /// </summary>
    public class ConnectedBindingContext : IBindingContext
    {
        private readonly ManualResetEvent bindingLock;

        private ServerConnection _serverConnection;

        /// <inheritdoc/>
        public IDataSource DataSource { get; set; }

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
                return this._serverConnection;
            }
            set
            {
                this._serverConnection = value;
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
    }
}
