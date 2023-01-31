//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Main class for Security Service functionality
    /// </summary>
    public sealed class SecurityService : IDisposable
    {
        private bool disposed;

        private ConnectionService connectionService = null;

        private static readonly Lazy<SecurityService> instance = new Lazy<SecurityService>(() => new SecurityService());

        /// <summary>
        /// Construct a new SecurityService instance with default parameters
        /// </summary>
        public SecurityService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static SecurityService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                connectionService ??= ConnectionService.Instance;
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
        /// Initializes the Security Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;

            // Credential request handlers
            this.ServiceHost.SetRequestHandler(CreateCredentialRequest.Type, HandleCreateCredentialRequest, true);
            this.ServiceHost.SetRequestHandler(UpdateCredentialRequest.Type, HandleUpdateCredentialRequest, true);
            this.ServiceHost.SetRequestHandler(DeleteCredentialRequest.Type, HandleDeleteCredentialRequest, true);
            this.ServiceHost.SetRequestHandler(GetCredentialsRequest.Type, HandleGetCredentialsRequest, true);

            // Login request handlers
            this.ServiceHost.SetRequestHandler(CreateLoginRequest.Type, HandleCreateLoginRequest, true);
            this.ServiceHost.SetRequestHandler(DeleteLoginRequest.Type, HandleDeleteLoginRequest, true);
        }


#region "Login Handlers"        

        /// <summary>
        /// Handle request to create a login
        /// </summary>
        internal async Task HandleCreateLoginRequest(CreateLoginParams parameters, RequestContext<CreateLoginResult> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
            // if (connInfo == null) 
            // {
            //     // raise an error
            // }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            LoginPrototype prototype = new LoginPrototype(dataContainer.Server, parameters.Login);

            if (prototype.LoginType == SqlServer.Management.Smo.LoginType.SqlLogin)
            {
                // check that there is a password
                // this check is made if policy enforcement is off
                // with policy turned on we do not display this message, instead we let server
                // return the error associated with null password (coming from policy) - see bug 124377
                if (prototype.SqlPassword.Length == 0 && prototype.EnforcePolicy == false)
                {
                    // raise error here                                                   
                }

                // check that password and confirm password controls' text matches
                if (0 != String.Compare(prototype.SqlPassword, prototype.SqlPasswordConfirm, StringComparison.Ordinal))
                {
                    // raise error here
                }                 
            }

            prototype.ApplyGeneralChanges(dataContainer.Server);

            await requestContext.SendResult(new CreateLoginResult()
            {
                Login = parameters.Login,
                Success = true,
                ErrorMessage = string.Empty
            });
        }

        /// <summary>
        /// Handle request to delete a credential
        /// </summary>
        internal async Task HandleDeleteLoginRequest(DeleteLoginParams parameters, RequestContext<ResultStatus> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
            // if (connInfo == null) 
            // {
            //     // raise an error
            // }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            Login login = dataContainer.Server.Logins[parameters.LoginName];
     
            dataContainer.SqlDialogSubject = login;
            DoDropObject(dataContainer);
           
            await requestContext.SendResult(new ResultStatus()
            {
                Success = true,
                ErrorMessage = string.Empty
            });
        }

#endregion

#region "User Handlers"
// ...
#endregion

#region "Credential Handlers"

        /// <summary>
        /// Handle request to create a credential
        /// </summary>
        internal async Task HandleCreateCredentialRequest(CreateCredentialParams parameters, RequestContext<CredentialResult> requestContext)
        {
            var result = await ConfigureCredential(parameters.OwnerUri,
                parameters.Credential,
                ConfigAction.Create,
                RunType.RunNow);

            await requestContext.SendResult(new CredentialResult()
            {
                Credential = parameters.Credential,
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        /// <summary>
        /// Handle request to update a credential
        /// </summary>
        internal async Task HandleUpdateCredentialRequest(UpdateCredentialParams parameters, RequestContext<CredentialResult> requestContext)
        {
            var result = await ConfigureCredential(parameters.OwnerUri,
                parameters.Credential,
                ConfigAction.Update,
                RunType.RunNow);

            await requestContext.SendResult(new CredentialResult()
            {
                Credential = parameters.Credential,
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        /// <summary>
        /// Handle request to delete a credential
        /// </summary>
        internal async Task HandleDeleteCredentialRequest(DeleteCredentialParams parameters, RequestContext<ResultStatus> requestContext)
        {
            var result = await ConfigureCredential(parameters.OwnerUri,
                parameters.Credential,
                ConfigAction.Drop,
                RunType.RunNow);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }


        /// <summary>
        /// Handle request to get all credentials
        /// </summary>
        internal async Task HandleGetCredentialsRequest(GetCredentialsParams parameters, RequestContext<GetCredentialsResult> requestContext)
        {
            var result = new GetCredentialsResult();
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
                CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                var credentials = dataContainer.Server.Credentials;
                int credentialsCount = credentials.Count;
                CredentialInfo[] credentialsInfos = new CredentialInfo[credentialsCount];
                for (int i = 0; i < credentialsCount; ++i)
                {
                    credentialsInfos[i] = new CredentialInfo();
                    credentialsInfos[i].Name = credentials[i].Name;
                    credentialsInfos[i].Identity = credentials[i].Identity;
                    credentialsInfos[i].Id = credentials[i].ID;
                    credentialsInfos[i].DateLastModified = credentials[i].DateLastModified;
                    credentialsInfos[i].CreateDate = credentials[i].CreateDate;
                    credentialsInfos[i].ProviderName = credentials[i].ProviderName;
                }
                result.Credentials = credentialsInfos;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.ToString();
            }

            await requestContext.SendResult(result);
        }

        /// <summary>
        /// Disposes the service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }

#endregion        

#region "Helpers"

        internal Task<Tuple<bool, string>> ConfigureCredential(
            string ownerUri,
            CredentialInfo credential,
            ConfigAction configAction,
            RunType runType)
        {
            return Task<Tuple<bool, string>>.Run(() =>
            {
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                    using (CredentialActions actions = new CredentialActions(dataContainer, credential, configAction))
                    {
                        var executionHandler = new ExecutonHandler(actions);
                        executionHandler.RunNow(runType, this);
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        /// <summary>
        /// this is the main method that is called by DropAllObjects for every object
        /// in the grid
        /// </summary>
        /// <param name="objectRowNumber"></param>
        private void DoDropObject(CDataContainer dataContainer)
        {            
            var executionMode = dataContainer.Server.ConnectionContext.SqlExecutionModes;
            var subjectExecutionMode = executionMode;

            //For Azure the ExecutionManager is different depending on which ExecutionManager
            //used - one at the Server level and one at the Database level. So to ensure we
            //don't use the wrong execution mode we need to set the mode for both (for on-prem
            //this will essentially be a no-op)
            SqlSmoObject sqlDialogSubject = null;
            try
            {
                sqlDialogSubject = dataContainer.SqlDialogSubject;
            }
            catch (System.Exception)
            {
                //We may not have a valid dialog subject here (such as if the object hasn't been created yet)
                //so in that case we'll just ignore it as that's a normal scenario. 
            }
            if (sqlDialogSubject != null)
            {
                subjectExecutionMode =
                    sqlDialogSubject.ExecutionManager.ConnectionContext.SqlExecutionModes;
            }

            Urn objUrn = sqlDialogSubject.Urn;
            System.Diagnostics.Debug.Assert(objUrn != null);

            SfcObjectQuery objectQuery = new SfcObjectQuery(dataContainer.Server);
           
            IDroppable droppableObj = null;
            string[] fields = null;

            foreach( object obj in objectQuery.ExecuteIterator( new SfcQueryExpression( objUrn.ToString() ), fields, null ) )
            {
                System.Diagnostics.Debug.Assert(droppableObj == null, "there is only one object");
                droppableObj = obj as IDroppable;
            }

            // For Azure databases, the SfcObjectQuery executions above may have overwritten our desired execution mode, so restore it
            dataContainer.Server.ConnectionContext.SqlExecutionModes = executionMode;
            if (sqlDialogSubject != null)
            {
                sqlDialogSubject.ExecutionManager.ConnectionContext.SqlExecutionModes = subjectExecutionMode;
            }

            if (droppableObj == null)
            {
                string objectName = objUrn.GetAttribute("Name");
                if(objectName == null)
                {
                    objectName = string.Empty;
                }
                throw new Microsoft.SqlServer.Management.Smo.MissingObjectException("DropObjectsSR.ObjectDoesNotExist(objUrn.Type, objectName)");
            }

            //special case database drop - see if we need to delete backup and restore history
            SpecialPreDropActionsForObject(dataContainer, droppableObj, 
                deleteBackupRestoreOrDisableAuditSpecOrDisableAudit: false,
                dropOpenConnections: false);

            droppableObj.Drop();

            //special case Resource Governor reconfigure - for pool, external pool, group  Drop(), we need to issue
            SpecialPostDropActionsForObject(dataContainer, droppableObj);

        }
        
        private void SpecialPreDropActionsForObject(CDataContainer dataContainer, IDroppable droppableObj, 
            bool deleteBackupRestoreOrDisableAuditSpecOrDisableAudit, bool dropOpenConnections)
        {
            Database db = droppableObj as Database;

            if (deleteBackupRestoreOrDisableAuditSpecOrDisableAudit)
            {
                if (db != null)
                {
                    dataContainer.Server.DeleteBackupHistory(db.Name);
                }
                else
                {
                    // else droppable object should be a server or database audit specification
                    ServerAuditSpecification sas = droppableObj as ServerAuditSpecification;
                    if (sas != null)
                    {
                        sas.Disable();
                    }
                    else
                    {
                        DatabaseAuditSpecification das = droppableObj as DatabaseAuditSpecification;
                        if (das != null)
                        {
                            das.Disable();
                        }
                        else
                        {
                            Audit aud = droppableObj as Audit;
                            if (aud != null)
                            {
                                aud.Disable();
                            }
                        }
                    }
                }
            }

            // special case database drop - drop existing connections to the database other than this one
            if (dropOpenConnections)
            {
                if (db.ActiveConnections > 0)
                {
                    // force the database to be single user
                    db.DatabaseOptions.UserAccess = DatabaseUserAccess.Single;
                    db.Alter(TerminationClause.RollbackTransactionsImmediately);
                }
            }
        }

        private void SpecialPostDropActionsForObject(CDataContainer dataContainer, IDroppable droppableObj)
        {
            if (droppableObj is Policy)
            {
                Policy policyToDrop = (Policy)droppableObj;
                if (!string.IsNullOrEmpty(policyToDrop.ObjectSet))
                {
                    ObjectSet objectSet = policyToDrop.Parent.ObjectSets[policyToDrop.ObjectSet];
                    objectSet.Drop();
                }
            }

            ResourcePool rp = droppableObj as ResourcePool;
            ExternalResourcePool erp = droppableObj as ExternalResourcePool;
            WorkloadGroup wg = droppableObj as WorkloadGroup;

            if (null != rp || null != erp || null != wg)
            {
                // Alter() Resource Governor to reconfigure
                dataContainer.Server.ResourceGovernor.Alter();
            }
        }

#endregion // "Helpers"
    }
}
