//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Utility;

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
        private readonly ConcurrentDictionary<string, FileBrowserOperation> ownerToFileBrowserMap = new ConcurrentDictionary<string, FileBrowserOperation>();
        private readonly ConcurrentDictionary<string, ValidatePathsCallback> validatePathsCallbackMap = new ConcurrentDictionary<string, ValidatePathsCallback>();
        private ConnectionService connectionService;
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
        /// Register validate path callback
        /// </summary>
        /// <param name="service"></param>
        /// <param name="callback"></param>
        public void RegisterValidatePathsCallback(string service, ValidatePathsCallback callback)
        {
            validatePathsCallbackMap.AddOrUpdate(service, callback, (key, oldValue) => callback);
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost">Service host to register handlers with</param>
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

        #region request handlers

        internal async Task HandleFileBrowserOpenRequest(FileBrowserOpenParams fileBrowserParams, RequestContext<bool> requestContext)
        {
            try
            {
                var task = Task.Run(() => RunFileBrowserOpenTask(fileBrowserParams, requestContext))
                    .ContinueWithOnFaulted(null);
                await requestContext.SendResult(true);
            }
            catch
            {
                await requestContext.SendResult(false);
            }
        }

        internal async Task HandleFileBrowserExpandRequest(FileBrowserExpandParams fileBrowserParams, RequestContext<bool> requestContext)
        {
            try
            {
                var task = Task.Run(() => RunFileBrowserExpandTask(fileBrowserParams, requestContext))
                    .ContinueWithOnFaulted(null);
                await requestContext.SendResult(true);
            }
            catch
            {
                await requestContext.SendResult(false);
            }
        }

        internal async Task HandleFileBrowserValidateRequest(FileBrowserValidateParams fileBrowserParams, RequestContext<bool> requestContext)
        {
            try
            {
                var task = Task.Run(() => RunFileBrowserValidateTask(fileBrowserParams, requestContext))
                    .ContinueWithOnFaulted(null);
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
            FileBrowserOperation removedOperation;
            response.Succeeded = ownerToFileBrowserMap.TryRemove(fileBrowserParams.OwnerUri, out removedOperation);
            if (removedOperation != null)
            {
                if (!removedOperation.FileTreeCreated)
                {
                    removedOperation.Cancel();
                }
                else
                {
                    removedOperation.SqlConnection.Close();
                }
            } 

            await requestContext.SendResult(response);
        }

        #endregion

        internal async Task RunFileBrowserOpenTask(FileBrowserOpenParams fileBrowserParams, RequestContext<bool> requestContext)
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
                    ownerToFileBrowserMap.AddOrUpdate(fileBrowserParams.OwnerUri, browser, (key, value) => browser);

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
                await requestContext.SendEvent(FileBrowserOpenedNotification.Type, result);
            }
        }

        internal async Task RunFileBrowserExpandTask(FileBrowserExpandParams fileBrowserParams, RequestContext<bool> requestContext)
        {
            FileBrowserExpandedParams result = new FileBrowserExpandedParams();
            try
            {
                FileBrowserOperation browser;
                result.Succeeded = ownerToFileBrowserMap.TryGetValue(fileBrowserParams.OwnerUri, out browser);
                if (result.Succeeded && browser != null)
                {
                    result.ExpandPath = fileBrowserParams.ExpandPath;
                    result.Children = browser.GetChildren(fileBrowserParams.ExpandPath).ToArray();
                    result.OwnerUri = fileBrowserParams.OwnerUri;
                }
            }
            catch (Exception ex)
            {
                result.Succeeded = false;
                result.Message = ex.Message;
            }

            await requestContext.SendEvent(FileBrowserExpandedNotification.Type, result);
        }

        internal async Task RunFileBrowserValidateTask(FileBrowserValidateParams fileBrowserParams, RequestContext<bool> requestContext)
        {
            FileBrowserValidatedParams result = new FileBrowserValidatedParams();

            try
            {
                ValidatePathsCallback callback;
                if (validatePathsCallbackMap.TryGetValue(fileBrowserParams.ServiceType, out callback)
                    && callback != null
                    && fileBrowserParams.SelectedFiles != null
                    && fileBrowserParams.SelectedFiles.Length > 0)
                {
                    string errorMessage;
                    result.Succeeded = callback(new FileBrowserValidateEventArgs
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

            await requestContext.SendEvent(FileBrowserValidatedNotification.Type, result);
        }
    }
}