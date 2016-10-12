//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
    /// The context used for binding requests
    /// </summary>
    public interface IBindingContext
    {
        /// <summary>
        /// Gets or sets a flag indicating if the context is connected
        /// </summary>
        bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the binding server connection
        /// </summary>
        ServerConnection ServerConnection { get; set; }

        /// <summary>
        /// Gets or sets the metadata display info provider
        /// </summary>
        MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }

        /// <summary>
        /// Gets or sets the SMO metadata provider
        /// </summary>
        SmoMetadataProvider SmoMetadataProvider { get; set; }

        /// <summary>
        /// Gets or sets the binder
        /// </summary>
        IBinder Binder { get; set; }

        /// <summary>
        /// Gets the binding lock object
        /// </summary>
        object BindingLock { get; }

        /// <summary>
        /// Gets or sets the binding operation timeout in milliseconds
        /// </summary>
        int BindingTimeout { get; set; }

        /// <summary>
        /// Gets or sets the current connection parse options
        /// </summary>
        ParseOptions ParseOptions { get; }

        /// <summary>
        /// Gets or sets the current connection server version
        /// </summary>
        ServerVersion ServerVersion { get; }

        /// <summary>
        /// Gets or sets the database engine type
        /// </summary>
        DatabaseEngineType DatabaseEngineType {  get; }

        /// <summary>
        /// Gets or sets the T-SQL version
        /// </summary>
        TransactSqlVersion TransactSqlVersion { get; }

        /// <summary>
        /// Gets or sets the database compatibility level
        /// </summary>
        DatabaseCompatibilityLevel DatabaseCompatibilityLevel { get; }        
    }
}
