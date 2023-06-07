//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using System;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class DatabasePropertiesHandler : ObjectTypeHandler<DatabasePropertiesInfo, DatabasePropertiesViewContext>
    {
        public DatabasePropertiesHandler(ConnectionService connectionService) : base(connectionService){ }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.DatabaseProperties;
        }


        public override Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams parameters)
        {
            //ConnectionInfo connInfo;
            //this.ConnectionService.TryFindConnection(parameters.ConnectionUri, out connInfo);
            //if (connInfo == null)
            //{
            //    throw new ArgumentException("Invalid ConnectionUri");
            //}
            //CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            //LoginViewInfo loginViewInfo = new LoginViewInfo();

            //// TODO cache databases and languages
            //string[] databases = new string[dataContainer.Server.Databases.Count];
            //for (int i = 0; i < dataContainer.Server.Databases.Count; i++)
            //{
            //    databases[i] = dataContainer.Server.Databases[i].Name;
            //}

            //var languageOptions = LanguageUtils.GetDefaultLanguageOptions(dataContainer);
            //var languageOptionsList = languageOptions.Select(LanguageUtils.FormatLanguageDisplay).ToList();
            //if (parameters.IsNewObject)
            //{
            //    languageOptionsList.Insert(0, SR.DefaultLanguagePlaceholder);
            //}
            //string[] languages = languageOptionsList.ToArray();
            //LoginPrototype prototype = parameters.IsNewObject
            //? new LoginPrototype(dataContainer)
            //: new LoginPrototype(dataContainer, dataContainer.Server.GetSmoObject(parameters.ObjectUrn) as Login);

            //List<string> loginServerRoles = new List<string>();
            //foreach (string role in prototype.ServerRoles.ServerRoleNames)
            //{
            //    if (prototype.ServerRoles.IsMember(role))
            //    {
            //        loginServerRoles.Add(role);
            //    }
            //}

            DatabasePropertiesInfo loginInfo = new DatabasePropertiesInfo()
            {
                Name = "xxxxx",
                CollationName = "xxxxx",
                DateCreated = "xxxxx",
                LastDatabaseBackup = "xxxxx",
                LastDatabaseLogBackup = "xxxxx",
                MemoryAllocatedToMemoryOptimizedObjects = "xxxxx",
                MemoryUsedByMemoryOptimizedObjects = "xxxxx",
                NumberOfUsers = "xxxxx",
                Owner = "xxxxx",
                Size= "xxxxx",
                SpaceAvailable = "xxxxx",
                Status = "xxxxx",
            };

            //var supportedAuthTypes = new List<LoginAuthenticationType>();
            //supportedAuthTypes.Add(LoginAuthenticationType.Sql);
            //if (prototype.WindowsAuthSupported)
            //{
            //    supportedAuthTypes.Add(LoginAuthenticationType.Windows);
            //}
            //if (prototype.AADAuthSupported)
            //{
            //    supportedAuthTypes.Add(LoginAuthenticationType.AAD);
            //}
            var viewInfo = new DatabasePropertiesViewInfo()
            {
                ObjectInfo = loginInfo,
            };
            //var context = new LoginViewContext(parameters);
            return Task.FromResult(new InitializeViewResult()
            {
                ViewInfo = new DatabasePropertiesViewInfo(),
                Context = new DatabasePropertiesViewContext(parameters)
            });
        }

        public override Task Save(DatabasePropertiesViewContext context, DatabasePropertiesInfo obj)
        {
            throw new NotImplementedException();
        }

        public override Task<string> Script(DatabasePropertiesViewContext context, DatabasePropertiesInfo obj)
        {
            throw new NotImplementedException();
        }
    }
}
