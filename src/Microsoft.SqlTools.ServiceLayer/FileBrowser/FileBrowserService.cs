//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    /// <summary>
    /// Main class for file browser service
    /// </summary>
    public sealed class FileBrowserService
    {
        private static readonly Lazy<FileBrowserService> LazyInstance = new Lazy<FileBrowserService>(() => new FileBrowserService());
        public static FileBrowserService Instance => LazyInstance.Value;

        // Cache file browser operations for expanding node request
        private Dictionary<string, FileBrowserOperation> ownerToFileBrowserMap = new Dictionary<string, FileBrowserOperation>();
        private Dictionary<string, ValidatePathsCallback> validatePathsCallbackMap = new Dictionary<string, ValidatePathsCallback>();
        private ConnectionService connectionService = null;
        private ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue(needsMetadata: false);

        /// <summary>
        /// Signature for callback method that validates the selected file paths
        /// </summary>
        /// <param name="eventArgs"></param>
        public delegate bool ValidatePathsCallback(FileBrowserValidateEventArgs eventArgs, out string errorMessage);

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
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public FileBrowserService()
        {
        }

        /// <summary>
        /// Register validate path callback
        /// </summary>
        /// <param name="service"></param>
        /// <param name="callback"></param>
        public void RegisterValidatePathsCallback(string service, ValidatePathsCallback callback)
        {
            if (this.validatePathsCallbackMap.ContainsKey(service))
            {
                this.validatePathsCallbackMap.Remove(service);
            }

            this.validatePathsCallbackMap.Add(service, callback);
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;

            // Open a file browser
            serviceHost.SetRequestHandler(FileBrowserOpenRequest.Type, HandleFileBrowserOpenRequest);

            // Expand a folder node
            serviceHost.SetRequestHandler(FileBrowserExpandRequest.Type, HandleFileBrowserExpandRequest);

            // Validate the selected files
            serviceHost.SetRequestHandler(FileBrowserValidateRequest.Type, HandleFileBrowserValidateRequest);

            // Close the file browser
            serviceHost.SetRequestHandler(FileBrowserCloseRequest.Type, HandleFileBrowserCloseRequest);
        }

        #region request handlers

        internal async Task HandleFileBrowserOpenRequest(
            FileBrowserOpenParams fileBrowserParams,
            RequestContext<bool> requestContext)
        {
            try
            {
                var task = Task.Run(() => RunFileBrowserOpenTask(fileBrowserParams));
                await requestContext.SendResult(true);
            }
            catch
            {
                await requestContext.SendResult(false);
            }
        }

        internal async Task HandleFileBrowserExpandRequest(
            FileBrowserExpandParams fileBrowserParams,
            RequestContext<bool> requestContext)
        {
            try
            {
                var task = Task.Run(() => RunFileBrowserExpandTask(fileBrowserParams));
                await requestContext.SendResult(true);
            }
            catch
            {
                await requestContext.SendResult(false);
            }
        }

        internal async Task HandleFileBrowserValidateRequest(
            FileBrowserValidateParams fileBrowserParams,
            RequestContext<bool> requestContext)
        {
            try
            {
                var task = Task.Run(() => RunFileBrowserValidateTask(fileBrowserParams));
                await requestContext.SendResult(true);
            }
            catch
            {
                await requestContext.SendResult(false);
            }
        }

        internal async Task HandleFileBrowserCloseRequest(
            FileBrowserCloseParams fileBrowserParams,
            RequestContext<FileBrowserCloseResponse> requestContext)
        {
            FileBrowserCloseResponse response = new FileBrowserCloseResponse();
            try
            {
                if (this.ownerToFileBrowserMap.ContainsKey(fileBrowserParams.OwnerUri))
                {
                    FileBrowserOperation browserOperation = this.ownerToFileBrowserMap[fileBrowserParams.OwnerUri];
                    if (!browserOperation.FileTreeCreated)
                    {
                        browserOperation.Cancel();
                    }
                    else
                    {
                        browserOperation.SqlConnection.Close();
                    }
                    this.ownerToFileBrowserMap.Remove(fileBrowserParams.OwnerUri);
                    response.Succeeded = true;
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }

            await requestContext.SendResult(response);
        }

        #endregion

        internal async Task RunFileBrowserOpenTask(FileBrowserOpenParams fileBrowserParams)
        {
            FileBrowserOpenedParams result = new FileBrowserOpenedParams();
            bool cancelRequested = false;

            try
            {
                ConnectionInfo connInfo;
                this.ConnectionServiceInstance.TryFindConnection(fileBrowserParams.OwnerUri, out connInfo);
                SqlConnection conn = null;

                if (connInfo != null)
                {
                    conn = ConnectionService.OpenSqlConnection(connInfo, "RemoteFileBrowser");
                }

                if (conn != null)
                {
                    FileBrowserOperation browser = new FileBrowserOperation(conn, fileBrowserParams.ExpandPath, fileBrowserParams.FileFilters);
                    if (this.ownerToFileBrowserMap.ContainsKey(fileBrowserParams.OwnerUri))
                    {
                        this.ownerToFileBrowserMap.Remove(fileBrowserParams.OwnerUri);
                    }
                    this.ownerToFileBrowserMap.Add(fileBrowserParams.OwnerUri, browser);

                    // Create file browser tree
                    browser.PopulateFileTree();

                    if (browser.IsCancellationRequested)
                    {
                        cancelRequested = true;
                        conn.Close();
                    }
                    else
                    {
                        result.OwnerUri = fileBrowserParams.OwnerUri;
                        result.FileTree = browser.FileTree;
                        result.Succeeded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }


            if (!cancelRequested)
            {
                await ServiceHost.SendEvent(FileBrowserOpenedNotification.Type, result);
            }
        }

        internal async Task RunFileBrowserExpandTask(FileBrowserExpandParams fileBrowserParams)
        {
            FileBrowserExpandedParams result = new FileBrowserExpandedParams();
            try
            {
                if (this.ownerToFileBrowserMap.ContainsKey(fileBrowserParams.OwnerUri))
                {
                    FileBrowserOperation browser = this.ownerToFileBrowserMap[fileBrowserParams.OwnerUri];
                    result.ExpandPath = fileBrowserParams.ExpandPath;
                    result.Children = browser.GetChildren(fileBrowserParams.ExpandPath).ToArray();
                    result.OwnerUri = fileBrowserParams.OwnerUri;
                    result.Succeeded = true;
                }
            }
            catch (Exception ex)
            {
                result.Succeeded = false;
                result.Message = ex.Message;
            }

            await ServiceHost.SendEvent(FileBrowserExpandedNotification.Type, result);
        }

        internal async Task RunFileBrowserValidateTask(FileBrowserValidateParams fileBrowserParams)
        {
            FileBrowserValidatedParams result = new FileBrowserValidatedParams();

            try
            {
                if (this.validatePathsCallbackMap.ContainsKey(fileBrowserParams.ServiceType)
                    && this.validatePathsCallbackMap[fileBrowserParams.ServiceType] != null
                    && fileBrowserParams.SelectedFiles != null
                    && fileBrowserParams.SelectedFiles.Length > 0)
                {
                    string errorMessage;
                    result.Succeeded = this.validatePathsCallbackMap[fileBrowserParams.ServiceType](new FileBrowserValidateEventArgs
                    {
                        ServiceType = fileBrowserParams.ServiceType,
                        OwnerUri = fileBrowserParams.OwnerUri,
                        FilePaths = fileBrowserParams.SelectedFiles
                    }, out errorMessage);

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        result.Message = errorMessage;
                    }
                }
                else
                {
                    result.Succeeded = true;
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            await ServiceHost.SendEvent(FileBrowserValidatedNotification.Type, result);
        }
    }
}