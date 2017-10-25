//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    /// <summary>
    /// Main class for file browser service
    /// </summary>
    public sealed class FileBrowserService: IDisposable
    {
        private static readonly Lazy<FileBrowserService> LazyInstance = new Lazy<FileBrowserService>(() => new FileBrowserService());
        public static FileBrowserService Instance => LazyInstance.Value;

        // Cache file browser operations for expanding node request
        private readonly ConcurrentDictionary<string, FileBrowserOperation> ownerToFileBrowserMap = new ConcurrentDictionary<string, FileBrowserOperation>();
        private readonly ConcurrentDictionary<string, ValidatePathsCallback> validatePathsCallbackMap = new ConcurrentDictionary<string, ValidatePathsCallback>();
        private ConnectionService connectionService;
        private ConnectedBindingQueue fileBrowserQueue = new ConnectedBindingQueue(needsMetadata: false);
        private static int DefaultTimeout = 120000;
        private string serviceName = "FileBrowser";

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
                    connectionService.RegisterConnectedQueue(this.serviceName, this.fileBrowserQueue);
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
            try
            {
                var task = Task.Run(() => RunFileBrowserCloseTask(fileBrowserParams, requestContext))
                    .ContinueWithOnFaulted(null);
                await requestContext.SendResult(new FileBrowserCloseResponse() { Succeeded = true });
            }
            catch
            {
                await requestContext.SendResult(new FileBrowserCloseResponse());
            }
        }

        #endregion

        public void Dispose()
        {
            this.fileBrowserQueue.Dispose();
        }

        internal async Task RunFileBrowserCloseTask(FileBrowserCloseParams fileBrowserParams, RequestContext<FileBrowserCloseResponse> requestContext)
        {
            FileBrowserCloseResponse result = new FileBrowserCloseResponse();
            try
            {
                FileBrowserOperation operation = null;
                ConnectionInfo connInfo;
                ownerToFileBrowserMap.TryGetValue(fileBrowserParams.OwnerUri, out operation);
                this.ConnectionServiceInstance.TryFindConnection(fileBrowserParams.OwnerUri, out connInfo);

                if (operation != null && connInfo != null)
                {
                    if (!operation.FileTreeCreated)
                    {
                        operation.Cancel();
                    }

                    // Clear queued items
                    this.fileBrowserQueue.ClearQueuedItems();

                    // Queue operation to clean up resources
                    QueueItem queueItem = fileBrowserQueue.QueueBindingOperation(
                        key: fileBrowserQueue.AddConnectionContext(connInfo, this.serviceName),
                        bindingTimeout: DefaultTimeout,
                        waitForLockTimeout: DefaultTimeout,
                        bindOperation: (bindingContext, cancelToken) =>
                        {
                            FileBrowserOperation removedOperation = null;
                            ownerToFileBrowserMap.TryRemove(fileBrowserParams.OwnerUri, out removedOperation);
                            if (removedOperation != null)
                            {
                                removedOperation.Dispose();
                            }
                            result.Succeeded = true;
                            return result;
                        });

                    queueItem.ItemProcessed.WaitOne();
                    if (queueItem.GetResultAsT<FileBrowserCloseResponse>() != null)
                    {
                        result = queueItem.GetResultAsT<FileBrowserCloseResponse>();
                    }

                    this.fileBrowserQueue.CloseConnections(connInfo.ConnectionDetails.ServerName, connInfo.ConnectionDetails.DatabaseName, DefaultTimeout);
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            await requestContext.SendEvent(FileBrowserClosedNotification.Type, result);
        }

        internal async Task RunFileBrowserOpenTask(FileBrowserOpenParams fileBrowserParams, RequestContext<bool> requestContext)
        {
            FileBrowserOpenedParams result = new FileBrowserOpenedParams();
            FileBrowserOperation browser = null;
            bool isCancelRequested = false;

            try
            {
                ConnectionInfo connInfo;
                this.ConnectionServiceInstance.TryFindConnection(fileBrowserParams.OwnerUri, out connInfo);
                if (connInfo != null)
                {
                    QueueItem queueItem = fileBrowserQueue.QueueBindingOperation(
                        key: fileBrowserQueue.AddConnectionContext(connInfo, this.serviceName),
                        bindingTimeout: DefaultTimeout,
                        waitForLockTimeout: DefaultTimeout,
                        bindOperation: (bindingContext, cancelToken) =>
                        {
                            if (!fileBrowserParams.ChangeFilter)
                            {
                                browser = new FileBrowserOperation(bindingContext.ServerConnection, fileBrowserParams.ExpandPath, fileBrowserParams.FileFilters);
                            }
                            else
                            {
                                ownerToFileBrowserMap.TryGetValue(fileBrowserParams.OwnerUri, out browser);
                                if (browser != null)
                                {
                                    browser.Initialize(fileBrowserParams.ExpandPath, fileBrowserParams.FileFilters);
                                }
                            }

                            if (browser != null)
                            {
                                ownerToFileBrowserMap.AddOrUpdate(fileBrowserParams.OwnerUri, browser, (key, value) => browser);

                                // Create file browser tree
                                browser.PopulateFileTree();

                                if (browser.IsCancellationRequested)
                                {
                                    isCancelRequested = true;
                                }
                                else
                                {
                                    result.OwnerUri = fileBrowserParams.OwnerUri;
                                    result.FileTree = browser.FileTree;
                                    result.Succeeded = true;
                                }
                            }

                            return result;
                        });

                    queueItem.ItemProcessed.WaitOne();

                    if (queueItem.GetResultAsT<FileBrowserOpenedParams>() != null)
                    {
                        result = queueItem.GetResultAsT<FileBrowserOpenedParams>();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            if (!isCancelRequested)
            {
                await requestContext.SendEvent(FileBrowserOpenedNotification.Type, result);
            }
        }

        internal async Task RunFileBrowserExpandTask(FileBrowserExpandParams fileBrowserParams, RequestContext<bool> requestContext)
        {
            FileBrowserExpandedParams result = new FileBrowserExpandedParams();
            try
            {
                FileBrowserOperation operation = null;
                ConnectionInfo connInfo;
                ownerToFileBrowserMap.TryGetValue(fileBrowserParams.OwnerUri, out operation);
                this.ConnectionServiceInstance.TryFindConnection(fileBrowserParams.OwnerUri, out connInfo);

                if (operation != null && connInfo != null)
                {
                    QueueItem queueItem = fileBrowserQueue.QueueBindingOperation(
                        key: fileBrowserQueue.AddConnectionContext(connInfo, this.serviceName),
                        bindingTimeout: DefaultTimeout,
                        waitForLockTimeout: DefaultTimeout,
                        bindOperation: (bindingContext, cancelToken) =>
                        {
                            result.ExpandPath = fileBrowserParams.ExpandPath;
                            result.Children = operation.GetChildren(fileBrowserParams.ExpandPath).ToArray();
                            result.OwnerUri = fileBrowserParams.OwnerUri;
                            result.Succeeded = true;
                            return result;
                        });

                    queueItem.ItemProcessed.WaitOne();

                    if (queueItem.GetResultAsT<FileBrowserExpandedParams>() != null)
                    {
                        result = queueItem.GetResultAsT<FileBrowserExpandedParams>();
                    }
                }
            }
            catch (Exception ex)
            {
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
                ConnectionInfo connInfo;
                this.ConnectionServiceInstance.TryFindConnection(fileBrowserParams.OwnerUri, out connInfo);
                if (validatePathsCallbackMap.TryGetValue(fileBrowserParams.ServiceType, out callback)
                    && callback != null
                    && connInfo != null
                    && fileBrowserParams.SelectedFiles != null
                    && fileBrowserParams.SelectedFiles.Length > 0)
                {
                    QueueItem queueItem = fileBrowserQueue.QueueBindingOperation(
                        key: fileBrowserQueue.AddConnectionContext(connInfo, this.serviceName),
                        bindingTimeout: DefaultTimeout,
                        waitForLockTimeout: DefaultTimeout,
                        bindOperation: (bindingContext, cancelToken) =>
                        {
                            string errorMessage;
                            result.Succeeded = callback(new FileBrowserValidateEventArgs
                            {
                                ServiceType = fileBrowserParams.ServiceType,
                                OwnerUri = fileBrowserParams.OwnerUri,
                                FilePaths = fileBrowserParams.SelectedFiles
                            }, out errorMessage);
                            result.Message = errorMessage;
                            return result;
                        });

                    queueItem.ItemProcessed.WaitOne();

                    if (queueItem.GetResultAsT<FileBrowserValidatedParams>() != null)
                    {
                        result = queueItem.GetResultAsT<FileBrowserValidatedParams>();
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