//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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
        public static SecurableTypeMetadata[] GetSecurableTypeMetadata(SqlObjectType objectType, Version serverVersion, string databaseName,DatabaseEngineType databaseEngineType, DatabaseEngineEdition engineEdition)
        {
            List<SecurableTypeMetadata> res = new List<SecurableTypeMetadata>();
            switch (objectType)
            {
                case SqlObjectType.ServerLevelLogin:
                case SqlObjectType.ServerRole:
                    AddSecurableTypeMetadata(res, securableTypesForServerLevel, serverVersion, databaseName, databaseEngineType, engineEdition);
                    break;
                case SqlObjectType.ApplicationRole:
                case SqlObjectType.DatabaseRole:
                case SqlObjectType.User:
                    AddSecurableTypeMetadata(res, (SearchableObjectType[])Enum.GetValues(typeof(SearchableObjectType)), serverVersion, databaseName, databaseEngineType, engineEdition);
                    break;
                default:
                    break;
            }
            return res.ToArray();
        }

        private static void AddSecurableTypeMetadata(List<SecurableTypeMetadata> res, SearchableObjectType[] supportedTypes, Version serverVersion, string databaseName,DatabaseEngineType databaseEngineType, DatabaseEngineEdition engineEdition)
        {
            foreach(SearchableObjectType t in supportedTypes)
            {
                if (t == SearchableObjectType.LastType)
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
                    DisplayName = desc.DisplayTypeNamePlural,
                    Permissions = permissions
                };
                res.Add(metadata);
            }
            res.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.InvariantCulture));
        }

        public static SecurablePermissions[] GetSecurablePermissions(bool principalExists, PrincipalType principalType, SqlSmoObject o, CDataContainer dataContainer)
        {
            List<SecurablePermissions> res = new List<SecurablePermissions>();
            Principal principal = CreatePrincipal(principalExists, principalType, o, dataContainer);
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
                            Grant = p?.State == PermissionStatus.Grant || p?.State == PermissionStatus.WithGrant,
                            WithGrant = p?.State == PermissionStatus.WithGrant
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
                    EffectivePermissions = GetEffectivePermissions(dataContainer)
                };
                res.Add(secPerm);
            }

            return res.ToArray();
        }

        public static string[] GetEffectivePermissions(CDataContainer dataContainer)
        {
            // TODO pass in other parameters for initialization
            var dataModel = new EffectivePermissionsData(dataContainer);
            List<string> res = new List<string>();
            DataSet data = dataModel.QueryEffectivePermissions();
            // STrace.Assert(data.Tables.Count == 1, "Unknown number of tables returned");

            if (data.Tables.Count > 0)
            {
                DataTable table = data.Tables[0];

                // STrace.Assert(table.Columns.Count >= 1 && table.Columns.Count <= 2, "Too many columns returned");

                bool hasColumnInformation = dataModel.HasColumnInformation;

                // loop through and add rows
                foreach (DataRow row in table.Rows)
                {
                    res.Add(row[0].ToString());
                }
            }
            return res.ToArray();
        }

        internal static Principal CreatePrincipal(bool principalExists, PrincipalType principalType, SqlSmoObject o, CDataContainer dataContainer)
        {
            Principal? principal = null;

            if (principalExists)
            {
                NamedSmoObject obj = (NamedSmoObject) o;
                principal = new Principal(obj, dataContainer.ConnectionInfo);
            }
            else
            {
                string objectName = dataContainer.ObjectName;
                Version serverVersion = Securable.GetServerVersion(dataContainer.ConnectionInfo);

                principal = new Principal(
                    objectName,
                    principalType,
                    principalExists,
                    dataContainer.ConnectionInfo,
                    serverVersion);
            }
            return principal;
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
    }
}