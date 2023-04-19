//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Credential object type handler
    /// </summary>
    public class CredentialHandler : ObjectTypeHandler
    {
        public CredentialHandler(ConnectionService connectionService) : base(connectionService)
        {
        }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.Credential;
        }

        public override Type GetObjectType()
        {
            return typeof(CredentialInfo);
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

        public override async Task Save(SqlObjectViewContext context, SqlObject obj)
        {
            var credential = obj as CredentialInfo;
            await ConfigureCredential(context.Parameters.ConnectionUri, credential, ConfigAction.Update, RunType.RunNow);
        }

        public override Task<string> Script(SqlObjectViewContext context, SqlObject obj)
        {
            throw new NotImplementedException();
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
    }
}