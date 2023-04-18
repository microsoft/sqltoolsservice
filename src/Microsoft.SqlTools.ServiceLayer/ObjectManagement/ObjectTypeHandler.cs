//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public abstract class ObjectTypeHandler
    {
        protected ConnectionService ConnectionService { get; }

        public ObjectTypeHandler(ConnectionService connectionService)
        {
            this.ConnectionService = connectionService;
        }

        public abstract bool CanHandleType(SqlObjectType objectType);
        public abstract SqlObjectViewInfo InitializeObjectView(Contracts.InitializeViewRequestParams requestParams, out ISqlObjectViewContext context);
        public abstract void Create(ISqlObjectViewContext context, SqlObject obj);
        public abstract void Update(ISqlObjectViewContext context, SqlObject obj);
        public abstract string Script(ISqlObjectViewContext context, SqlObject obj);
        public abstract Type GetObjectType();

        public virtual void Rename(string connectionUri, string objectUrn, string newName)
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
        }

        public virtual void Drop(string connectionUri, string objectUrn, bool throwIfNotExist)
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