//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.FileValidator;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    /// <summary>
    /// Main class for file browser service
    /// </summary>
    public sealed class FileBrowserService
    {
        private static readonly Lazy<FileBrowserService> LazyInstance = new Lazy<FileBrowserService>(() => new FileBrowserService());
        public static FileBrowserService Instance => LazyInstance.Value;

        // Caches file browser operation for expanding node request
        private Dictionary<string, FileBrowserOperation> ownerToFileBrowserMap = new Dictionary<string, FileBrowserOperation>();
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
            // Open a file browser
            serviceHost.SetRequestHandler(FileBrowserOpenRequest.Type, HandleFileBrowserOpenRequest);

            // Expand a folder node
            serviceHost.SetRequestHandler(FileBrowserExpandRequest.Type, HandleFileBrowserExpandRequest);

            // Validate the selected files
            serviceHost.SetRequestHandler(FileBrowserValidateRequest.Type, HandleFileBrowserValidateRequest);

            // Close the file browser
            serviceHost.SetRequestHandler(FileBrowserCloseRequest.Type, HandleFileBrowserCloseRequest);
        }

        internal async Task HandleFileBrowserOpenRequest(
            FileBrowserOpenParams fileBrowserParams,
            RequestContext<FileBrowserOpenResponse> requestContext)
        {
            FileBrowserOpenResponse response = new FileBrowserOpenResponse();

            try
            {
                ConnectionInfo connInfo;
                this.ConnectionServiceInstance.TryFindConnection(
                    fileBrowserParams.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    SqlConnection sqlConn = GetSqlConnection(connInfo);
                    FileBrowserOperation browser = new FileBrowserOperation(sqlConn, fileBrowserParams.ExpandPath, fileBrowserParams.FileFilters);
                    browser.PopulateFileTree();

                    // Cache the file browser operation for expanding node request in the future
                    this.ownerToFileBrowserMap.Add(fileBrowserParams.OwnerUri, browser);

                    response.FileTree = browser.FileTree;
                    response.Succeeded = true;
                }
            }
            catch (Exception ex)
            {
                response.Succeeded = false;
                response.Message = ex.Message;
            }

            await requestContext.SendResult(response);
        }

        internal async Task HandleFileBrowserExpandRequest(
            FileBrowserExpandParams fileBrowserParams,
            RequestContext<FileBrowserExpandResponse> requestContext)
        {
            FileBrowserExpandResponse response = new FileBrowserExpandResponse();

            try
            {
                FileBrowserOperation browser = this.ownerToFileBrowserMap[fileBrowserParams.OwnerUri];
                if (browser != null)
                {
                    browser.ExpandSelectedNode(fileBrowserParams.ExpandPath);
                    response.ExpandedNode = browser.FileTree.SelectedNode;
                    response.Succeeded = true;
                }
            }
            catch (Exception ex)
            {
                response.Succeeded = false;
                response.Message = ex.Message;
            }

            await requestContext.SendResult(response);
        }

        internal async Task HandleFileBrowserValidateRequest(
            FileBrowserValidateParams fileBrowserParams,
            RequestContext<FileBrowserValidateResponse> requestContext)
        {
            FileBrowserValidateResponse response = new FileBrowserValidateResponse();

            try
            {
                if (fileBrowserParams.SelectedFiles != null && fileBrowserParams.SelectedFiles.Length > 0)
                {
                    IFileValidator fileValidator = null;
                    switch (fileBrowserParams.ServiceType)
                    {
                        case ServiceConstants.Backup:
                        case ServiceConstants.Restore:
                            ConnectionInfo connInfo;
                            this.ConnectionServiceInstance.TryFindConnection(
                                fileBrowserParams.OwnerUri,
                                out connInfo);
                            SqlConnection sqlConn = GetSqlConnection(connInfo);
                            fileValidator = new DisasterRecoveryFileValidator(sqlConn,
                                fileBrowserParams.ServiceType == ServiceConstants.Restore);
                            break;
                        default:
                            break;
                    }

                    if (fileValidator != null)
                    {
                        string errorMessage = "";
                        fileValidator.ValidatePaths(fileBrowserParams.SelectedFiles, out errorMessage);
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            response.Succeeded = false;
                            response.Message = errorMessage;
                        }
                        else
                        {
                            response.Succeeded = true;
                        }
                    }
                    else
                    {
                        response.Succeeded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                response.Succeeded = false;
                response.Message = ex.Message;
            }

            await requestContext.SendResult(response);
        }

        internal async Task HandleFileBrowserCloseRequest(
            FileBrowserCloseParams fileBrowserParams,
            RequestContext<FileBrowserCloseResponse> requestContext)
        {
            FileBrowserCloseResponse response = new FileBrowserCloseResponse();
            if (this.ownerToFileBrowserMap.ContainsKey(fileBrowserParams.OwnerUri))
            {
                this.ownerToFileBrowserMap.Remove(fileBrowserParams.OwnerUri);
                response.Succeeded = true;
            }
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