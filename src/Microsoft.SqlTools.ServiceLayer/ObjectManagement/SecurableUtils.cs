//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.PermissionsData;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class SecurableUtils
    {
        public static SecurableTypeMetadata[] GetSecurableTypeMetadata(SqlObjectType objectType, Version serverVersion, string databaseName,DatabaseEngineType databaseEngineType, DatabaseEngineEdition engineEdition)
        {
            List<SecurableTypeMetadata> res = new List<SecurableTypeMetadata>();
            switch (objectType)
            {
                case SqlObjectType.ApplicationRole:
                case SqlObjectType.DatabaseRole:
                case SqlObjectType.ServerRole:
                case SqlObjectType.ServerLevelLogin:
                case SqlObjectType.User:
                    AddAllSecurableTypeMetadata(res, serverVersion, databaseName, databaseEngineType, engineEdition);
                    break;
                default:
                    break;
            }
            return res.ToArray();
        }

        private static void AddAllSecurableTypeMetadata(List<SecurableTypeMetadata> res, Version serverVersion, string databaseName,DatabaseEngineType databaseEngineType, DatabaseEngineEdition engineEdition)
        {
            foreach(SearchableObjectType t in Enum.GetValues(typeof(SearchableObjectType)))
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
                List<SecurablePermissionItem> permissionItems = new List<SecurablePermissionItem>();
                for (int i = 0; i < permissionStates.Count; i++)
                {
                    var p = permissionStates[i];
                    var permissionItem = new SecurablePermissionItem()
                    {
                        Permission = p?.Permission.Name,
                        Grantor = p?.Grantor,
                        Grant = p?.State == PermissionStatus.Grant || p?.State == PermissionStatus.WithGrant,
                        WithGrant = p?.State == PermissionStatus.WithGrant
                    };
                    permissionItems.Add(permissionItem);
                }
                // var distinct = permissionItems.GroupBy(p => p.Permission).Select(g => {
                //     if (g.Count() > 1)
                //     {
                //         foreach (var item in g)
                //         {
                //             if (item.Grantor != null)
                //             {
                //                 return item;
                //             }
                //         }
                //     }
                //     return g.First();
                // }).ToArray();

                SecurablePermissions secPerm = new SecurablePermissions()
                {
                    Name = s.Name,
                    Schema = s.Schema,
                    Type = s.TypeName,
                    Permissions = permissionItems.ToArray(),
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
    }
}