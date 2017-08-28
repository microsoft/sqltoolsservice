using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    /// <summary>
    /// Main class for remote file browser service functionality
    /// </summary>
    public sealed class FileBrowserService
    {
        private static readonly Lazy<FileBrowserService> LazyInstance = new Lazy<FileBrowserService>(() => new FileBrowserService());

        public static FileBrowserService Instance => LazyInstance.Value;

        private ConnectionService connectionService = null;

        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }
            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(FileBrowserOpenRequest.Type, HandleFileBrowserOpenRequest);
            serviceHost.SetRequestHandler(FileBrowserFilterRequest.Type, HandleFileBrowserFilterRequest);
            serviceHost.SetRequestHandler(FileBrowserExpandRequest.Type, HandleFileBrowserExpandRequest);
            serviceHost.SetRequestHandler(FileBrowserCloseRequest.Type, HandleFileBrowserCloseRequest);
        }

        internal async Task HandleFileBrowserOpenRequest(
            FileBrowserParams fileBrowserParams,
            RequestContext<FileBrowserResponse> requestContext)
        {
            // open a connection and cache it!!
            FileBrowserResponse response = new FileBrowserResponse();

            try
            {
                ConnectionInfo connInfo;
                this.ConnectionServiceInstance.TryFindConnection(
                    fileBrowserParams.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    SqlConnection sqlConn = GetSqlConnection(connInfo);
                    ServerConnection serverConnection = new ServerConnection(sqlConn);
                    FileBrowserOperation browser = new FileBrowserOperation(serverConnection, fileBrowserParams.ExpandPath, fileBrowserParams.FileFilters);
                }

                response.FileTree = new FileTree();
                response.Result = true;
                await requestContext.SendResult(response);
            }
            catch (Exception ex)
            {
                response.Result = false;
                response.Message = ex.ToString();
                await requestContext.SendResult(response);
            }
        }

        internal async Task HandleFileBrowserFilterRequest(
            FileBrowserParams fileBrowserParams,
            RequestContext<FileBrowserResponse> requestContext)
        {
            FileBrowserResponse response = new FileBrowserResponse();
            await requestContext.SendResult(response);
        }

        internal async Task HandleFileBrowserExpandRequest(
            FileBrowserParams fileBrowserParams,
            RequestContext<FileBrowserExpandResponse> requestContext)
        {
            FileBrowserExpandResponse response = new FileBrowserExpandResponse();
            await requestContext.SendResult(response);
        }

        internal async Task HandleFileBrowserCloseRequest(
            FileBrowserCloseParams fileBrowserParams,
            RequestContext<FileBrowserCloseResponse> requestContext)
        {
            // dispose the connection!!

            // do the validation
            if (fileBrowserParams.selectedFiles != null && fileBrowserParams.selectedFiles.Length > 0)
            {
                ConnectionInfo connInfo;
                this.ConnectionServiceInstance.TryFindConnection(
                    fileBrowserParams.OwnerUri,
                    out connInfo);

                SqlConnection sqlConn = GetSqlConnection(connInfo);
                ServerConnection serverConnection = new ServerConnection(sqlConn);

                IFileValidator fileValidator = null;
                switch (fileBrowserParams.ServiceType)
                {
                    case ServiceTypes.Backup:
                        fileValidator = new BackupFileValidator(serverConnection);
                        break;
                    default:
                        break;
                }

                string errorMessage = "";
                fileValidator.ValidatePaths(fileBrowserParams.selectedFiles, out errorMessage);
            }

            FileBrowserCloseResponse response = new FileBrowserCloseResponse();
            await requestContext.SendResult(response);
        }

        internal static SqlConnection GetSqlConnection(ConnectionInfo connInfo)
        {
            try
            {
                // increase the connection timeout to at least 30 seconds and and build connection string
                // enable PersistSecurityInfo to handle issues in SMO where the connection context is lost in reconnections
                int? originalTimeout = connInfo.ConnectionDetails.ConnectTimeout;
                bool? originalPersistSecurityInfo = connInfo.ConnectionDetails.PersistSecurityInfo;
                connInfo.ConnectionDetails.ConnectTimeout = Math.Max(30, originalTimeout ?? 0);
                connInfo.ConnectionDetails.PersistSecurityInfo = true;
                string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                connInfo.ConnectionDetails.ConnectTimeout = originalTimeout;
                connInfo.ConnectionDetails.PersistSecurityInfo = originalPersistSecurityInfo;

                // open a dedicated binding server connection
                SqlConnection sqlConn = new SqlConnection(connectionString);
                sqlConn.Open();
                return sqlConn;
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}