//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#nullable disable

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Credential object type handler
    /// </summary>
    public class CredentialHandler : ObjectTypeHandler<CredentialInfo, CredentialViewContext>
    {
        public CredentialHandler(ConnectionService connectionService) : base(connectionService)
        {
        }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.Credential;
        }

        public override Task<InitializeViewResult> InitializeObjectView(Contracts.InitializeViewRequestParams parameters)
        {
            // TODO: this is partially implemented only.
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(parameters.ConnectionUri, out connInfo);
            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

            var credentialInfo = new CredentialInfo();
            if (!parameters.IsNewObject)
            {
                var credential = dataContainer.Server.GetSmoObject(parameters.ObjectUrn) as Credential;
                credentialInfo.Name = credential.Name;
                credentialInfo.Identity = credential.Identity;
                credentialInfo.Id = credential.ID;
                credentialInfo.DateLastModified = credential.DateLastModified;
                credentialInfo.CreateDate = credential.CreateDate;
                credentialInfo.ProviderName = credential.ProviderName;
            }
            var viewInfo = new CredentialViewInfo() { ObjectInfo = credentialInfo };
            var context = new CredentialViewContext(parameters);
            var result = new InitializeViewResult { ViewInfo = viewInfo, Context = context };
            return Task.FromResult(result);
        }

        public override async Task Save(CredentialViewContext context, CredentialInfo obj)
        {
            await ConfigureCredential(context.Parameters.ConnectionUri, obj, ConfigAction.Update, RunType.RunNow);
        }

        public override Task<string> Script(CredentialViewContext context, CredentialInfo obj)
        {
            throw new NotImplementedException();
        }

        public async Task Create(Contracts.CreateCredentialRequestParams parameters)
        {
            await ConfigureCredential(parameters.ConnectionUri, parameters.CredentialInfo, ConfigAction.Create, RunType.RunNow);
        }

        public List<string> GetCredentials(Contracts.GetCredentialsRequestParams parameters)
        {
            List<string> credentials = new List<string>();
            ConnectionInfo connectionInfo = this.GetConnectionInfo(parameters.connectionUri);
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionInfo))
            {
                if (sqlConn != null)
                {
                    using (var cmd = new SqlCommand { Connection = sqlConn })
                    {
                        cmd.CommandText = "SELECT [NAME] FROM sys.credentials";
                        cmd.ExecuteNonQuery();
                        using (IDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                credentials.Add(reader.GetString(0));  
                            }
                        }
                    }
                }
            }
            return credentials;
        }

        private Task<Tuple<bool, string>> ConfigureCredential(string ownerUri, CredentialInfo credential, ConfigAction configAction, RunType runType)
        {
            return Task<Tuple<bool, string>>.Run(() =>
            {
                try
                {
                    ConnectionInfo connInfo;
                    this.ConnectionService.TryFindConnection(ownerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                    using (CredentialActions actions = new CredentialActions(dataContainer, credential, configAction))
                    {
                        var executionHandler = new ExecutionHandler(actions);
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
    }
}