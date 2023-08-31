//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Moq;
using Newtonsoft.Json.Linq;
using DatabaseFile = Microsoft.SqlTools.ServiceLayer.ObjectManagement.DatabaseFile;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
{
    public static class ObjectManagementTestUtils
    {
        private static ObjectManagementService _objectManagementService;

        static ObjectManagementTestUtils()
        {
            ObjectManagementService.ConnectionServiceInstance = LiveConnectionHelper.GetLiveTestConnectionService();
            _objectManagementService = new ObjectManagementService();
        }

        internal static ObjectManagementService Service
        {
            get
            {
                return _objectManagementService;
            }
        }

        public static string TestCredentialName = "Current User";

        internal static string GetCurrentUserIdentity()
        {
            return string.Format(@"{0}\{1}", Environment.UserDomainName, Environment.UserName);
        }

        internal static string GetDatabaseURN(string name)
        {
            return string.Format("Server/Database[@Name='{0}']", name);
        }

        internal static string GetLoginURN(string name)
        {
            return string.Format("Server/Login[@Name='{0}']", name);
        }

        internal static string GetUserURN(string database, string name)
        {
            return string.Format("Server/Database[@Name='{0}']/User[@Name='{1}']", database, name);
        }

        internal static string GetCredentialURN(string name)
        {
            return string.Format("Server/Credential[@Name = '{0}']", name);
        }

        internal static DatabaseInfo GetTestDatabaseInfo()
        {
            return new DatabaseInfo()
            {
                Name = "TestDatabaseName_" + new Random().NextInt64(10000000, 90000000).ToString(),
                Owner = "sa",
                CollationName = "SQL_Latin1_General_CP1_CI_AS",
                CompatibilityLevel = "SQL Server 2022 (160)",
                ContainmentType = "None",
                RecoveryModel = "Full",
                LastDatabaseBackup = "None",
                LastDatabaseLogBackup = "None",
                PageVerify = "CHECKSUM",
                RestrictAccess = "MULTI_USER",
                AutoCreateIncrementalStatistics = true,
                AutoCreateStatistics = true,
                AutoShrink = false,
                AutoUpdateStatistics = true,
                AutoUpdateStatisticsAsynchronously = false,
                DatabaseScopedConfigurations = null
            };
        }

        internal static LoginInfo GetTestLoginInfo()
        {
            return new LoginInfo()
            {
                Name = "TestLoginName_" + new Random().NextInt64(10000000, 90000000).ToString(),
                AuthenticationType = LoginAuthenticationType.Sql,
                WindowsGrantAccess = true,
                MustChangePassword = false,
                IsEnabled = false,
                IsLockedOut = false,
                EnforcePasswordPolicy = false,
                EnforcePasswordExpiration = false,
                Password = "placeholder" + new Random().NextInt64(10000000, 90000000).ToString() + "!*PLACEHOLDER",
                OldPassword = "placeholder" + new Random().NextInt64(10000000, 90000000).ToString() + "!*PLACEHOLDER",
                DefaultLanguage = "English - us_english",
                DefaultDatabase = "master",
                SecurablePermissions = new SecurablePermissions[0]
            };
        }

        internal static DatabaseFile[] GetTestDatabaseFiles()
        {
            List<DatabaseFile> databaseFiles = new List<DatabaseFile>() {
                new DatabaseFile() {
                        Id = 0,
                        Name = "TestDatabaseName_File1_" + new Random().NextInt64(10000000, 90000000).ToString(),
                        Type = "LOG",
                        AutoFileGrowth = 100,
                        AutoFileGrowthType = FileGrowthType.KB,
                        FileGroup = "Not Applicable",
                        FileNameWithExtension = "",
                        SizeInMb = 10,
                        IsAutoGrowthEnabled = true,
                        MaxSizeLimitInMb = -1,
                        Path = ""
                    }
            };
            return databaseFiles.ToArray();
        }

        internal static List<FileGroupSummary> GetTestDatabaseFilegroups()
        {
            List<FileGroupSummary> fgs = new List<FileGroupSummary>();
            fgs.Add(new FileGroupSummary()
            {
                Id = -1,
                Name = "rowFilegroup1",
                IsDefault = false,
                IsReadOnly = false,
                AutogrowAllFiles = true,
                Type = FileGroupType.RowsFileGroup
            });
            fgs.Add(new FileGroupSummary()
            {
                Id = -2,
                Name = "memOptFg1",
                Type = FileGroupType.MemoryOptimizedDataFileGroup
            });
            return fgs;
        }

            internal static UserInfo GetTestUserInfo(DatabaseUserType userType, string userName = null, string loginName = null)
        {
            return new UserInfo()
            {
                Type = userType,
                Name = userName ?? "TestUserName_" + new Random().NextInt64(10000000, 90000000).ToString(),
                LoginName = loginName,
                Password = "placeholder" + new Random().NextInt64(10000000, 90000000).ToString() + "!*PLACEHOLDER",
                DefaultSchema = "dbo",
                OwnedSchemas = new string[] { "" },
                SecurablePermissions = new SecurablePermissions[0]
            };
        }

        internal static CredentialInfo GetTestCredentialInfo()
        {
            return new CredentialInfo()
            {
                Identity = GetCurrentUserIdentity(),
                Name = TestCredentialName
            };
        }

        internal static InitializeViewRequestParams GetInitializeViewRequestParams(string connectionUri, string database, bool isNewObject, SqlObjectType objectType, string parentUrn, string objectUrn)
        {
            return new InitializeViewRequestParams()
            {
                ConnectionUri = connectionUri,
                Database = database,
                IsNewObject = isNewObject,
                ObjectType = objectType,
                ContextId = Guid.NewGuid().ToString(),
                ParentUrn = parentUrn,
                ObjectUrn = objectUrn
            };
        }

        internal static async Task SaveObject(InitializeViewRequestParams parameters, SqlObject obj)
        {
            // Initialize the view
            var initViewRequestContext = new Mock<RequestContext<SqlObjectViewInfo>>();
            initViewRequestContext.Setup(x => x.SendResult(It.IsAny<SqlObjectViewInfo>()))
                .Returns(Task.FromResult<SqlObjectViewInfo>(null));
            await Service.HandleInitializeViewRequest(parameters, initViewRequestContext.Object);

            // Save the object
            var saveObjectRequestContext = new Mock<RequestContext<SaveObjectRequestResponse>>();
            saveObjectRequestContext.Setup(x => x.SendResult(It.IsAny<SaveObjectRequestResponse>()))
                .Returns(Task.FromResult<SaveObjectRequestResponse>(new SaveObjectRequestResponse()));
            await Service.HandleSaveObjectRequest(new SaveObjectRequestParams { ContextId = parameters.ContextId, Object = JToken.FromObject(obj) }, saveObjectRequestContext.Object);

            // Dispose the view
            var disposeViewRequestContext = new Mock<RequestContext<DisposeViewRequestResponse>>();
            disposeViewRequestContext.Setup(x => x.SendResult(It.IsAny<DisposeViewRequestResponse>()))
                .Returns(Task.FromResult<DisposeViewRequestResponse>(new DisposeViewRequestResponse()));
            await Service.HandleDisposeViewRequest(new DisposeViewRequestParams { ContextId = parameters.ContextId }, disposeViewRequestContext.Object);
        }

        internal static async Task<DatabaseViewInfo> GetDatabaseObject(InitializeViewRequestParams parameters, SqlObject obj)
        {
            // Initialize the view
            DatabaseViewInfo databaseViewInfo = new DatabaseViewInfo();
            var initViewRequestContext = new Mock<RequestContext<SqlObjectViewInfo>>();
            initViewRequestContext
                .Setup(x => x.SendResult(It.IsAny<SqlObjectViewInfo>()))
                .Returns(Task.FromResult<SqlObjectViewInfo>(null))
                .Callback<DatabaseViewInfo>(r => databaseViewInfo = r);
            await Service.HandleInitializeViewRequest(parameters, initViewRequestContext.Object);

            // Dispose the view
            var disposeViewRequestContext = new Mock<RequestContext<DisposeViewRequestResponse>>();
            disposeViewRequestContext.Setup(x => x.SendResult(It.IsAny<DisposeViewRequestResponse>()))
                .Returns(Task.FromResult<DisposeViewRequestResponse>(new DisposeViewRequestResponse()));
            await Service.HandleDisposeViewRequest(new DisposeViewRequestParams { ContextId = parameters.ContextId }, disposeViewRequestContext.Object);

            return databaseViewInfo;
        }

        internal static async Task<string> ScriptObject(InitializeViewRequestParams parameters, SqlObject obj)
        {
            // Initialize the view
            var initViewRequestContext = new Mock<RequestContext<SqlObjectViewInfo>>();
            initViewRequestContext.Setup(x => x.SendResult(It.IsAny<SqlObjectViewInfo>()))
                .Returns(Task.FromResult<SqlObjectViewInfo>(null));
            await Service.HandleInitializeViewRequest(parameters, initViewRequestContext.Object);

            // Script the object
            string script = string.Empty;
            var scriptObjectRequestContext = new Mock<RequestContext<string>>();
            scriptObjectRequestContext
                .Setup(x => x.SendResult(It.IsAny<string>()))
                .Returns(Task.FromResult<string>(""))
                .Callback<string>(scriptResult => script = scriptResult);
            await Service.HandleScriptObjectRequest(new ScriptObjectRequestParams { ContextId = parameters.ContextId, Object = JToken.FromObject(obj) }, scriptObjectRequestContext.Object);

            // Dispose the view
            var disposeViewRequestContext = new Mock<RequestContext<DisposeViewRequestResponse>>();
            disposeViewRequestContext.Setup(x => x.SendResult(It.IsAny<DisposeViewRequestResponse>()))
                .Returns(Task.FromResult<DisposeViewRequestResponse>(new DisposeViewRequestResponse()));
            await Service.HandleDisposeViewRequest(new DisposeViewRequestParams { ContextId = parameters.ContextId }, disposeViewRequestContext.Object);

            return script;
        }

        internal static async Task DropObject(string connectionUri, string objectUrn, bool throwIfNotExist = false)
        {
            var dropParams = new DropRequestParams
            {
                ConnectionUri = connectionUri,
                ObjectUrn = objectUrn,
                ThrowIfNotExist = throwIfNotExist
            };

            var dropRequestContext = new Mock<RequestContext<DropRequestResponse>>();
            dropRequestContext.Setup(x => x.SendResult(It.IsAny<DropRequestResponse>()))
                .Returns(Task.FromResult(new DropRequestResponse()));

            await Service.HandleDropRequest(dropParams, dropRequestContext.Object);
        }

        internal static async Task<LoginInfo> CreateTestLogin(string connectionUri)
        {
            var testLogin = GetTestLoginInfo();
            var parametersForCreation = GetInitializeViewRequestParams(connectionUri, "master", true, SqlObjectType.ServerLevelLogin, "", "");
            await SaveObject(parametersForCreation, testLogin);
            return testLogin;
        }

        internal static async Task<UserInfo> CreateTestUser(string connectionUri, DatabaseUserType userType,
            string userName = null,
            string loginName = null,
            string databaseName = "master",
            bool scriptUser = false)
        {
            var testUser = GetTestUserInfo(userType, userName, loginName);
            var parametersForCreation = GetInitializeViewRequestParams(connectionUri, databaseName, true, SqlObjectType.User, "", "");
            await SaveObject(parametersForCreation, testUser);
            return testUser;
        }

        internal static async Task<CredentialInfo> SetupCredential(string connectionUri)
        {
            var credential = GetTestCredentialInfo();
            var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionUri, "master", true, SqlObjectType.Credential, "", "");
            await DropObject(connectionUri, GetCredentialURN(credential.Name));
            await ObjectManagementTestUtils.SaveObject(parametersForCreation, credential);
            return credential;
        }

        internal static async Task CleanupCredential(string connectionUri, CredentialInfo credential)
        {
            await DropObject(connectionUri, GetCredentialURN(credential.Name));
        }
    }
}
