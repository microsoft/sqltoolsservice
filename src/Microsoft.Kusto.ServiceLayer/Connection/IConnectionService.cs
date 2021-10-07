using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.Kusto.ServiceLayer.Connection
{
    public interface IConnectionService
    {
        /// <summary>
        /// Register a new connection queue if not already registered
        /// </summary>
        /// <param name="type"></param>
        /// <param name="connectedQueue"></param>
        void RegisterConnectedQueue(string type, IConnectedBindingQueue connectedQueue);

        /// <summary>
        /// Open a connection with the specified ConnectParams
        /// </summary>
        Task<ConnectionCompleteParams> Connect(ConnectParams connectionParams);

        /// <summary>
        /// Gets the existing connection with the given URI and connection type string. If none exists, 
        /// creates a new connection. This cannot be used to create a default connection or to create a 
        /// connection if a default connection does not exist.
        /// </summary>
        /// <param name="ownerUri">URI identifying the resource mapped to this connection</param>
        /// <param name="connectionType">
        /// What the purpose for this connection is. A single resource
        /// such as a SQL file may have multiple connections - one for Intellisense, another for query execution
        /// </param>
        /// <param name="alwaysPersistSecurity">
        /// Workaround for .Net Core clone connection issues: should persist security be used so that
        /// when SMO clones connections it can do so without breaking on SQL Password connections.
        /// This should be removed once the core issue is resolved and clone works as expected
        /// </param>
        /// <returns>A DB connection for the connection type requested</returns>
        Task<ReliableDataSourceConnection> GetOrOpenConnection(string ownerUri, string connectionType, bool alwaysPersistSecurity = false);

        /// <summary>
        /// Cancel a connection that is in the process of opening.
        /// </summary>
        bool CancelConnect(CancelConnectParams cancelParams);

        /// <summary>
        /// Close a connection with the specified connection details.
        /// </summary>
        bool Disconnect(DisconnectParams disconnectParams);

        void InitializeService(IProtocolEndpoint serviceHost, IDataSourceConnectionFactory dataSourceConnectionFactory, 
            IConnectedBindingQueue connectedBindingQueue, IConnectionManager connectionManager);

        /// <summary> 
        /// Add a new method to be called when the onconnection request is submitted 
        /// </summary> 
        /// <param name="activity"></param> 
        void RegisterOnConnectionTask(ConnectionService.OnConnectionHandler activity);

        /// <summary>
        /// Add a new method to be called when the ondisconnect request is submitted
        /// </summary>
        void RegisterOnDisconnectTask(ConnectionService.OnDisconnectHandler activity);

        /// <summary>
        /// Handles a request to get a connection string for the provided connection
        /// </summary>
        Task HandleGetConnectionStringRequest(
            GetConnectionStringParams connStringParams,
            RequestContext<string> requestContext);

        /// <summary>
        /// Handles a request to serialize a connection string
        /// </summary>
        Task HandleBuildConnectionInfoRequest(
            string connectionString,
            RequestContext<ConnectionDetails> requestContext);

        ConnectionDetails ParseConnectionString(string connectionString);
    }
}