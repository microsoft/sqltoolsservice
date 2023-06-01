//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.PermissionsData;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class SecurableUtils
    {
        private static readonly SearchableObjectType[] securableTypesForServerLevel = new SearchableObjectType[] {
            SearchableObjectType.AvailabilityGroup,
            SearchableObjectType.Endpoint,
            SearchableObjectType.Login,
            SearchableObjectType.ServerRole,
            SearchableObjectType.Server
        };

        private static readonly SearchableObjectType[] securableTypesForDbLevel = new SearchableObjectType[] {
            SearchableObjectType.AggregateFunction,
            SearchableObjectType.ApplicationRole,
            SearchableObjectType.Assembly,
            SearchableObjectType.AsymmetricKey,
            SearchableObjectType.Certificate,
            SearchableObjectType.Database,
            SearchableObjectType.DatabaseRole,
            SearchableObjectType.ExternalDataSource,
            SearchableObjectType.ExternalFileFormat,
            SearchableObjectType.FullTextCatalog,
            SearchableObjectType.FunctionInline,
            SearchableObjectType.ServiceQueue,
            SearchableObjectType.FunctionScalar,
            SearchableObjectType.Schema,
            SearchableObjectType.SecurityPolicy,
            SearchableObjectType.Sequence,
            SearchableObjectType.StoredProcedure,
            SearchableObjectType.SymmetricKey,
            SearchableObjectType.Synonym,
            SearchableObjectType.Table,
            SearchableObjectType.FunctionTable,
            SearchableObjectType.UserDefinedDataType,
            SearchableObjectType.UserDefinedTableType,
            SearchableObjectType.User,
            SearchableObjectType.View,
            SearchableObjectType.XmlSchemaCollection
        };

        static internal string launchEffectivePermissions = @"
<formdescription>
    <params>
        <servername></servername>
        <servertype>sql</servertype>
        <database></database>
        <urn></urn>
        <executeas></executeas>
        <executetype></executetype>
        <A32942B7-FBDE-4ac3-B84E-F5EC89961094 />
        <assemblyname>sqlmgmt.dll</assemblyname>
    </params>
</formdescription>";

        public static SecurableTypeMetadata[] GetSecurableTypeMetadata(SqlObjectType objectType, Version serverVersion, string databaseName,DatabaseEngineType databaseEngineType, DatabaseEngineEdition engineEdition)
        {
            List<SecurableTypeMetadata> res = new List<SecurableTypeMetadata>();
            switch (objectType)
            {
                case SqlObjectType.ServerLevelLogin:
                case SqlObjectType.ServerRole:
                    AddSecurableTypeMetadata(res, securableTypesForServerLevel, null, serverVersion, databaseName, databaseEngineType, engineEdition);
                    break;
                case SqlObjectType.ApplicationRole:
                case SqlObjectType.DatabaseRole:
                case SqlObjectType.User:
                    AddSecurableTypeMetadata(res, securableTypesForDbLevel, databaseEngineType == DatabaseEngineType.SqlAzureDatabase ? new SearchableObjectType[] {SearchableObjectType.ServiceQueue} : null, serverVersion, databaseName, databaseEngineType, engineEdition);
                    break;
                default:
                    break;
            }
            return res.ToArray();
        }

        private static void AddSecurableTypeMetadata(List<SecurableTypeMetadata> res, SearchableObjectType[] supportedTypes, SearchableObjectType[]? excludeList, Version serverVersion, string databaseName,DatabaseEngineType databaseEngineType, DatabaseEngineEdition engineEdition)
        {
            foreach(SearchableObjectType t in supportedTypes)
            {
                if (t == SearchableObjectType.LastType || (excludeList != null && excludeList.Contains(t)))
                {
                    continue;
                }
                SecurableType secType = PermissionsData.Securable.GetSecurableType(t);
                SearchableObjectTypeDescription desc = SearchableObjectTypeDescription.GetDescription(t);
                var pList = PermissionsData.Securable.GetRelevantPermissions(secType, serverVersion, databaseName, databaseEngineType, engineEdition);
                var permissions = new PermissionMetadata[pList.Count];
                for (int i = 0; i < pList.Count; i++)
                {
                    var p = (Permission)pList[i];
                    permissions[i] = new PermissionMetadata()
                    {
                        Name = p?.Name,
                        DisplayName = p?.Name
                    };
                }

                SecurableTypeMetadata metadata = new SecurableTypeMetadata()
                {
                    Name = desc.DisplayTypeNameSingular,
                    DisplayName = desc.DisplayTypeNameSingular,
                    Permissions = permissions
                };
                res.Add(metadata);
            }
            res.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.InvariantCulture));
        }

        public static SecurablePermissions[] GetSecurablePermissions(bool principalExists, PrincipalType principalType, SqlSmoObject o, CDataContainer dataContainer)
        {
            if (principalType == PrincipalType.Login && dataContainer?.Server?.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return new SecurablePermissions[0];
            }

            List<SecurablePermissions> res = new List<SecurablePermissions>();
            Principal principal;

            try
            {
                principal = CreatePrincipal(principalExists, principalType, o, null, dataContainer);
            }
            catch(Exception)
            {
                return new SecurablePermissions[0];
            }

            principal.AddExistingSecurables();

            var securables = principal.GetSecurables(new SecurableComparer(SecurableComparer.DefaultSortingOrder, true));
            foreach (Securable s in securables)
            {
                var permissionStates = principal.GetPermissionStates(s);
                Dictionary<string, SecurablePermissionItem> permissionItemsDict = new Dictionary<string, SecurablePermissionItem>();
                for (int i = 0; i < permissionStates.Count; i++)
                {
                    var p = permissionStates[i];
                    string key = p?.Permission.Name ?? string.Empty;
                    if (!permissionItemsDict.ContainsKey(key) || string.IsNullOrEmpty(permissionItemsDict[key].Grantor))
                    {
                        var permissionItem = new SecurablePermissionItem()
                        {
                            Permission = p?.Permission.Name,
                            Grantor = p?.Grantor,
                            Grant = p?.State == PermissionStatus.Revoke ? null : p?.State == PermissionStatus.Grant || p?.State == PermissionStatus.WithGrant,
                            WithGrant = p?.State == PermissionStatus.Revoke ? null : p?.State == PermissionStatus.WithGrant,
                        };
                        permissionItemsDict[key] = permissionItem;
                    }
                }

                var permissions = permissionItemsDict.Values.OrderBy(x => x.Permission, StringComparer.InvariantCulture).ToArray();

                SecurablePermissions secPerm = new SecurablePermissions()
                {
                    Name = s.Name,
                    Schema = s.Schema,
                    Type = s.TypeName,
                    Permissions = permissions,
                    EffectivePermissions = CanHaveEffectivePermissions(principalType, dataContainer) ? GetEffectivePermissions(dataContainer, s, principal) : new string[0]
                };
                res.Add(secPerm);
            }

            return res.ToArray();
        }

        public static bool CanHaveEffectivePermissions(PrincipalType principalType, CDataContainer dataContainer)
        {
            if (dataContainer?.Server?.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return false;
            }

            if (principalType == PrincipalType.ServerRole || principalType == PrincipalType.DatabaseRole || principalType == PrincipalType.ApplicationRole)
            {
                return false;
            }
            return true;
        }

        internal static string[] GetEffectivePermissions(CDataContainer dataContainer, Securable securable, Principal principal)
        {
            var doc = ReadEffectivePermissionsXml(securable, principal);
            dataContainer.Document = doc;
            var dataModel = new EffectivePermissionsData(dataContainer);
            List<string> res = new List<string>();
            DataSet data = dataModel.QueryEffectivePermissions();

            if (data.Tables.Count > 0)
            {
                DataTable table = data.Tables[0];


                bool hasColumnInformation = dataModel.HasColumnInformation;

                // loop through and add rows
                foreach (DataRow row in table.Rows)
                {
                    if (hasColumnInformation && !string.IsNullOrEmpty(row[1].ToString()))
                    {
                        continue;
                    }
                    res.Add(row[0].ToString());
                }
            }
            return res.ToArray();
        }

        /// <summary>
        /// Form the xml to query effective permissions data 
        /// </summary>
        /// <returns></returns>
        private static XmlDocument ReadEffectivePermissionsXml(Securable securable, Principal principal )
        {
            if (securable != null && principal != null)
            {
                string executeas = null;
                string executetype = null;
                GetPrincipalToExecuteAs(principal,
                                                                     securable.DatabaseName,
                                                                     securable.ConnectionInfo,
                                                                     out executeas,
                                                                     out executetype);

                // build a document
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(launchEffectivePermissions);
                xml.SelectSingleNode("/formdescription/params/urn").InnerText = securable.Urn;
                xml.SelectSingleNode("/formdescription/params/servername").InnerText = ((SqlConnectionInfo)securable.ConnectionInfo).ServerName;
                xml.SelectSingleNode("/formdescription/params/database").InnerText = securable.DatabaseName;
                xml.SelectSingleNode("/formdescription/params/executeas").InnerText = executeas;
                xml.SelectSingleNode("/formdescription/params/executetype").InnerText = executetype;

                return xml;
            }

            return null;
        }

        /// <summary>
        /// Create principal object for server level principals
        /// </summary>
        internal static Principal CreatePrincipal(bool principalExists, PrincipalType principalType, SqlSmoObject o, string? objectName, CDataContainer dataContainer)
        {
            if (principalExists)
            {
                NamedSmoObject obj = (NamedSmoObject) o;
                return new Principal(obj, dataContainer.ConnectionInfo);
            }
            else
            {
                Version serverVersion = Securable.GetServerVersion(dataContainer.ConnectionInfo);

                return new Principal(
                    objectName,
                    principalType,
                    principalExists,
                    dataContainer.ConnectionInfo,
                    serverVersion);
            }
        }

        /// <summary>
        /// Create principal object for database level principals
        /// </summary>
        internal static Principal CreatePrincipal(bool principalExists, PrincipalType principalType, SqlSmoObject o, string? objectName, CDataContainer dataContainer, string databaseName)
        {
            if (principalExists)
            {
                NamedSmoObject obj = (NamedSmoObject)o;
                return new Principal(obj, dataContainer.ConnectionInfo);
            }
            else
            {
                Version serverVersion = Securable.GetServerVersion(dataContainer.ConnectionInfo);
                DatabaseEngineType databaseEngineType = Securable.GetDatabaseEngineType(dataContainer.ConnectionInfo);
                DatabaseEngineEdition databaseEngineEdition = Securable.GetDatabaseEngineEdition(dataContainer.ConnectionInfo);
                return new Principal(
                                              objectName,
                                              databaseName,
                                              principalType,
                                              principalExists,
                                              dataContainer.ConnectionInfo,
                                              serverVersion, 
                                              databaseEngineType,
                                              databaseEngineEdition);
            }
        }

        public static String EscapeString(String s, string esc)
        {
            if (null == s)
            {
                return null;
            }
            string replace = esc + esc;
            StringBuilder sb = new StringBuilder(s);
            sb.Replace(esc, replace);
            return sb.ToString();
        }

        internal static SearchableObjectType ConvertPotentialSqlObjectTypeToSearchableObjectType(string typeStr)
        {
            if (typeStr == ConvertSqlObjectTypeToStringValue(SqlObjectType.ApplicationRole))
            {
                return SearchableObjectType.ApplicationRole;
            }
            else if (typeStr == ConvertSqlObjectTypeToStringValue(SqlObjectType.Credential))
            {
                return SearchableObjectType.Credential;
            }
            else if (typeStr == ConvertSqlObjectTypeToStringValue(SqlObjectType.DatabaseRole))
            {
                return SearchableObjectType.DatabaseRole;
            }
            else if (typeStr == ConvertSqlObjectTypeToStringValue(SqlObjectType.ServerLevelLogin))
            {
                return SearchableObjectType.Login;
            }
            else if (typeStr == ConvertSqlObjectTypeToStringValue(SqlObjectType.ServerRole))
            {
                return SearchableObjectType.ServerRole;
            }
            else if (typeStr == ConvertSqlObjectTypeToStringValue(SqlObjectType.Table))
            {
                return SearchableObjectType.Table;
            }
            else if (typeStr == ConvertSqlObjectTypeToStringValue(SqlObjectType.User))
            {
                return SearchableObjectType.User;
            }
            else if (typeStr == ConvertSqlObjectTypeToStringValue(SqlObjectType.View))
            {
                return SearchableObjectType.View;
            }
            else
            {
                return ConvertStringToSearchableObjectType(typeStr);
            }
        }

        private static string ConvertSqlObjectTypeToStringValue(SqlObjectType objectType)
        {
            return JsonConvert.SerializeObject(objectType).Replace("\"", "");
        }

        private static SearchableObjectType ConvertStringToSearchableObjectType(string typeStr)
        {
            foreach(SearchableObjectType t in Enum.GetValues(typeof(SearchableObjectType)))
            {
                if (t == SearchableObjectType.LastType)
                {
                    continue;
                }
                SecurableType secType = PermissionsData.Securable.GetSecurableType(t);
                SearchableObjectTypeDescription desc = SearchableObjectTypeDescription.GetDescription(t);
                if (desc.DisplayTypeNameSingular == typeStr || desc.DisplayTypeNamePlural == typeStr)
                {
                    return t;
                }
            }
            return SearchableObjectType.LastType;
        }

        internal static void GetPrincipalToExecuteAs(Principal principal,
                                                     string databaseName,
                                                     object connectionInfo,
                                                     out string executeas,
                                                     out string executetype)
        {
            executeas = null;
            executetype = null;

            //
            // IF we are a user AND we are mapped to a login, 
            // then we actually want to calculate effective 
            // permissions as the login, and not as the user.
            // why?  because that's the only way the server 
            // level perms will be taken into account.
            //
            if (principal.PrincipalType == PrincipalType.User &&
                !string.IsNullOrEmpty(databaseName))
            {
                SqlConnectionInfoWithConnection ci = connectionInfo as SqlConnectionInfoWithConnection;
                if (ci != null && ci.ServerConnection != null)
                {
                    Server server = new Server(ci.ServerConnection);
                    if (server != null)
                    {
                        Database db = server.Databases[databaseName];
                        if (db != null)
                        {
                            User u = db.Users[principal.Name];

                            //
                            // if the the user is mapped to a certificate or
                            // or asymmetric key, we should execute as user.
                            //
                            if (u != null &&
                                (u.LoginType == LoginType.SqlLogin ||
                                 u.LoginType == LoginType.WindowsUser ||
                                 u.LoginType == LoginType.WindowsGroup) &&
                                !string.IsNullOrEmpty(u.Login))
                            {
                                executeas = u.Login;
                                executetype = "login";
                            }
                        }
                    }
                }
            }

            //
            // if we couldn't determine what type of user the principal was, 
            // or if the user is mapped to a certificate or asymmetric key, or if
            // the principal was a login, we will default to executing as whatever
            // principal type we are (either login or user).
            //
            if (string.IsNullOrEmpty(executeas) || string.IsNullOrEmpty(executetype))
            {
                executeas = principal.Name;
                executetype = (principal.PrincipalType == PrincipalType.Login) ? "login" : "user";
            }
        }

        internal static SearchableObject ConvertFromSecurableNameToSearchableObject(string securableName, string type, string database, string schema, object connectionInfo)
        {
            SearchableObjectType searchableObjectType = ConvertPotentialSqlObjectTypeToSearchableObjectType(type);

            SearchableObjectTypeDescription desc = SearchableObjectTypeDescription.GetDescription(searchableObjectType);
            SearchableObjectCollection results = new SearchableObjectCollection();

            if (desc.IsDatabaseObject)
            {
                if (desc.IsSchemaObject)
                {
                    SearchableObject.Search(
                            results,
                            searchableObjectType,
                            connectionInfo,
                            database,
                            securableName,
                            true,
                            schema,
                            true,
                            true);
                }
                else
                {
                    SearchableObject.Search(
                        results,
                        searchableObjectType,
                        connectionInfo,
                        database,
                        securableName,
                        true,
                        true);
                }
            }
            else
            {
                SearchableObject.Search(
                    results,
                    searchableObjectType,
                    connectionInfo,
                    securableName,
                    true,
                    true);
            }
            SearchableObject result = (results.Count != 0) ? results[0] : null;
            return result;
        }

        internal static void SendToServerPermissionChanges(bool exists, string name, SecurablePermissions[] securablePermissions, Principal principal, CDataContainer dataContainer, string database)
        {
            if (securablePermissions == null)
            {
                return;
            }

            if (principal.PrincipalType == PrincipalType.Login && dataContainer.Server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return;
            }

            if (!exists)
            {
                foreach (SecurablePermissions secPerm in securablePermissions)
                {
                    var securable = principal.AddSecurable(SecurableUtils.ConvertFromSecurableNameToSearchableObject(secPerm.Name, secPerm.Type, database, secPerm.Schema, dataContainer.ConnectionInfo));
                    var states = principal.GetPermissionStates(securable);
                    ApplyPermissionStates(secPerm.Permissions, states);
                }
            }
            else
            {
                var securables = principal.GetSecurables(new SecurableComparer(SecurableComparer.DefaultSortingOrder, true));
                foreach (SecurablePermissions secPerm in securablePermissions)
                {
                    var securable = FindMatchedSecurable(securables, secPerm.Name) ?? principal.AddSecurable(SecurableUtils.ConvertFromSecurableNameToSearchableObject(secPerm.Name, secPerm.Type, database, secPerm.Schema, dataContainer.ConnectionInfo));
                    var states = principal.GetPermissionStates(securable);
                    ApplyPermissionStates(secPerm.Permissions, states);                
                }

                var newSecurableNames = securablePermissions.Select(s => s.Name).ToHashSet();
                foreach (Securable securable in securables)
                {
                    if (!newSecurableNames.Contains(securable.Name))
                    {
                        var states = principal.GetPermissionStates(securable);
                        for (int i = 0; i < states.Count; i++)
                        {
                            states[i].Revoke();
                        }
                        principal.RemoveSecurable(securable);
                    }
                }
            }
            principal.ApplyChanges(name, dataContainer.Server);
        }

        private static Securable FindMatchedSecurable(SecurableList securableList, string name)
        {
            foreach (Securable securable in securableList)
            {
                if (securable.Name == name)
                {
                    return securable;
                }
            }
            return null;
        }

        private static void ApplyPermissionStates(SecurablePermissionItem[] items, PermissionStateCollection states)
        {
            foreach (var p in items)
            {
                var key = p.Permission + p.Grantor;
                if (p.WithGrant == true)
                {
                    states[key].State = PermissionStatus.WithGrant;
                }
                else if (p.Grant == true)
                {
                    states[key].State = PermissionStatus.Grant;
                }
                else if (p.Grant == false)
                {
                    states[key].State = PermissionStatus.Deny;
                }
                else if (p.Grant == null)
                {
                    states[key].State = PermissionStatus.Revoke;
                }
            }
            var itemNames = items.Select(item => item.Permission).ToHashSet();

            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (!itemNames.Contains(state.Permission.Name))
                {
                    state.State = PermissionStatus.Revoke;
                }
            }
        }
    }
}