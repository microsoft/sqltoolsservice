//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public interface IObjectTypeHandler
    {
        bool CanHandleType(SqlObjectType objectType);
        Task<InitializeViewResult> InitializeObjectView(Contracts.InitializeViewRequestParams requestParams);
        Task Save(SqlObjectViewContext context, SqlObject obj);
        Task<string> Script(SqlObjectViewContext context, SqlObject obj);
        Type GetObjectType();
        Task Rename(string connectionUri, string objectUrn, string newName);
        Task Drop(string connectionUri, string objectUrn, bool throwIfNotExist);
    }

    public abstract class ObjectTypeHandler<ObjectType, ContextType> : IObjectTypeHandler
    where ObjectType : SqlObject
    where ContextType : SqlObjectViewContext
    {
        protected ConnectionService ConnectionService { get; }

        public ObjectTypeHandler(ConnectionService connectionService)
        {
            this.ConnectionService = connectionService;
        }

        public abstract bool CanHandleType(SqlObjectType objectType);
        public abstract Task<InitializeViewResult> InitializeObjectView(Contracts.InitializeViewRequestParams requestParams);
        public abstract Task Save(ContextType context, ObjectType obj);
        public abstract Task<string> Script(ContextType context, ObjectType obj);

        public Task Save(SqlObjectViewContext context, SqlObject obj)
        {
            return this.Save((ContextType)context, (ObjectType)obj);
        }

        public Task<string> Script(SqlObjectViewContext context, SqlObject obj)
        {
            return this.Script((ContextType)context, (ObjectType)obj);
        }

        public Type GetObjectType()
        {
            return typeof(ObjectType);
        }

        public virtual Task Rename(string connectionUri, string objectUrn, string newName)
        {
            ConnectionInfo connInfo = this.GetConnectionInfo(connectionUri);
            ServerConnection serverConnection = ConnectionService.OpenServerConnection(connInfo, ObjectManagementService.ApplicationName);
            using (serverConnection.SqlConnectionObject)
            {
                Server server = new Server(serverConnection);
                SqlSmoObject dbObject = server.GetSmoObject(new Urn(objectUrn));
                var renamable = dbObject as IRenamable;
                if (renamable != null)
                {
                    renamable.Rename(newName);
                }
                else
                {
                    throw new Exception(SR.ObjectNotRenamable(objectUrn));
                }
            }
            return Task.CompletedTask;
        }

        public virtual Task Drop(string connectionUri, string objectUrn, bool throwIfNotExist)
        {
            ConnectionInfo connectionInfo = this.GetConnectionInfo(connectionUri);
            using (CDataContainer dataContainer = CDataContainer.CreateDataContainer(connectionInfo, databaseExists: true))
            {
                try
                {
                    dataContainer.SqlDialogSubject = dataContainer.Server?.GetSmoObject(objectUrn);
                    DatabaseUtils.DoDropObject(dataContainer);
                }
                catch (FailedOperationException ex)
                {
                    if (!(ex.InnerException is MissingObjectException) || (ex.InnerException is MissingObjectException && throwIfNotExist))
                    {
                        throw;
                    }
                }
            }
            return Task.CompletedTask;
        }

        protected ConnectionInfo GetConnectionInfo(string connectionUri)
        {
            ConnectionInfo connInfo;
            if (this.ConnectionService.TryFindConnection(connectionUri, out connInfo))
            {
                return connInfo;
            }
            else
            {
                Logger.Error($"The connection with URI '{connectionUri}' could not be found.");
                throw new Exception(SR.ErrorConnectionNotFound);
            }
        }
    }
}