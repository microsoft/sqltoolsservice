//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlTools.LanguageService.Connection.Contracts;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Delegate invoked when a connection is established.
    /// </summary>
    public delegate Task ConnectionHandler(ConnectionInfoBase info);

    /// <summary>
    /// Delegate invoked when a connection is closed.
    /// </summary>
    public delegate Task DisconnectionHandler(IConnectionSummary summary, string ownerUri);

    /// <summary>
    /// Connection service surface required by the language service. The hosting service layer
    /// implements this so the language service can be decoupled from the concrete connection service.
    /// </summary>
    public interface IConnectionService
    {
        /// <summary>
        /// Whether the SQL authentication provider is enabled (instance-scoped).
        /// </summary>
        bool EnableSqlAuthenticationProvider { get; }

        /// <summary>
        /// Whether connection pooling is enabled.
        /// </summary>
        bool EnableConnectionPooling { get; }

        /// <summary>
        /// Tracks owner URIs that have an in-flight token refresh request.
        /// </summary>
        ConcurrentDictionary<string, bool> TokenUpdateUris { get; }

        /// <summary>
        /// Registers a binding queue for the given connection type.
        /// </summary>
        void RegisterConnectedQueue(string type, IConnectedBindingQueue connectedQueue);

        /// <summary>
        /// Registers a task to run when a connection is established.
        /// </summary>
        void RegisterOnConnectionTask(ConnectionHandler activity);

        /// <summary>
        /// Registers a task to run when a connection is disconnected.
        /// </summary>
        void RegisterOnDisconnectTask(DisconnectionHandler activity);

        /// <summary>
        /// Requests a refreshed auth token for the connection if one is needed.
        /// </summary>
        Task<bool> TryRequestRefreshAuthToken(string ownerUri);

        /// <summary>
        /// Tries to find the connection info for the given owner URI.
        /// </summary>
        bool TryFindConnection(string ownerUri, out ConnectionInfoBase connectionInfo);

        /// <summary>
        /// Updates the cached access token for the given connection.
        /// </summary>
        void UpdateAuthToken(string uri, string token, int expiresOn);
    }
}
