//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Rename.Requests;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Rename
{
    /// <summary>
    /// Main class for Rename Service functionality
    /// </summary>
    public class RenameService : IDisposable
    {
        private bool disposed = false;

        private static Lazy<RenameService> renameServiceInstance = new Lazy<RenameService>(() => new RenameService());
        public static RenameService Instance => renameServiceInstance.Value;

        public static ConnectionService connectionService;

        private IProtocolEndpoint serviceHost;

        public RenameService() { }
        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
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

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            this.serviceHost = serviceHost;
            this.serviceHost.SetRequestHandler(ProcessRenameEditRequest.Type, HandleProcessRenameEditRequest);

        }
        internal Task HandleRequest<T>(RequestContext<T> requestContext, Func<Task> action)
        {
            // The request handling will take some time to return, we need to use a separate task to run the request handler so that it won't block the main thread.
            // For any specific table designer instance, ADS UI can make sure there are at most one request being processed at any given time, so we don't have to worry about race conditions.
            return Task.Run(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
        }
        /// <summary>
        /// Method to handle the renaming operation
        /// </summary>
        /// <param name="requestParams">parameters which are needed to execute renaming operation</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        internal Task HandleProcessRenameEditRequest(ProcessRenameEditRequestParams requestParams, RequestContext<bool> requestContext)
        {
            return this.HandleRequest<bool>(requestContext, async () =>
           {
               Logger.Verbose("Handle Request in ProcessRenameEditRequest()");
               bool operationExecutedSuccessFull = false;
               RenameUtils.Validate(requestParams);
               ConnectionInfo connInfo;
               try
               {
                   connectionService.TryFindConnection(
                          requestParams.TableInfo.OwnerUri,
                          out connInfo);

                   using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "RenamingDatabaseObjects"))
                   {
                       ExecuteRenaming(requestParams, sqlConn);
                   }
                   operationExecutedSuccessFull = true;
               }
               catch (Exception e)
               {
                   Logger.Error("Error on executing renaming operation: " + e.ToString());
                   throw new InvalidOperationException("The renaming operation was not successfull executed");
               }
               await requestContext.SendResult(operationExecutedSuccessFull);
           });
        }

        /// <summary>
        /// Method to execute the renaming operation on the server
        /// </summary>
        /// <param name="requestParams">parameters which are needed to execute renaming operation</param>
        /// <param name="sqlConn">the sqlconnection on which the operation is executed</param>
        private void ExecuteRenaming(ProcessRenameEditRequestParams requestParams, SqlConnection sqlConn)
        {
            Logger.Verbose("Inside in the ExecuteRenaming()-Method");
            IRenamable renameObject = RenameUtils.GetSQLRenameObject(requestParams, sqlConn);
            try
            {
                renameObject.Rename(requestParams.ChangeInfo.NewName);
            }
            catch (Exception e)
            {
                Logger.Error("Error on renaming operation: " + e);
                throw new InvalidOperationException("The renaming operation was not successfull executed");
            }
            Logger.Verbose("Exiting the ExecuteRenaming()-Method");
        }
        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }
    }
}