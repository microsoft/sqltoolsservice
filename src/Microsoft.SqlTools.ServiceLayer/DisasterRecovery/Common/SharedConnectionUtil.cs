//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
//using Microsoft.SqlServer.Management.AzureCredential;
using Microsoft.SqlServer.Management.Common;
//using Microsoft.SqlServer.Management.Smo.RegSvrEnum;
using SFC = Microsoft.SqlServer.Management.Sdk.Sfc;
//using Microsoft.SqlServer.StorageClient;
//using Microsoft.SqlServer.Management.SqlMgmt;

/// <summary>
/// Summary description for SharedConectionUtil
/// Moved GetConnectionName static call in a public class acessible for both 
/// OEXM and OE
/// </summary>
namespace Microsoft.SqlTools.ServiceLayer.Common
{
    internal class SharedConnectionUtil
    {
        public SharedConnectionUtil()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ci"></param>
        /// <returns></returns>
        public static string GetConnectionKeyName(SqlOlapConnectionInfoBase ci)
        {

            //// Note that these strings are not localized. The returned string is used by OE in a
            //// hash of connections so it can tell if it already has such a connection open. This
            //// string is never seen by the user. For the string seen by the user, see
            //// ServerNameHandler.cs.
            string displayName = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} (", ci.ServerName);

            if (!string.IsNullOrEmpty(ci.DatabaseName))
            {
                displayName += ", " + ci.DatabaseName;
            }

            return displayName;

            //switch (ci.ServerType)
            //{
            //    case ConnectionType.AzureStorage:
            //        AzureStorageConnectionInfo azureCI = ci as AzureStorageConnectionInfo;
            //        displayName = "AzureStorage," + azureCI.BlobClient.BaseUri;
            //        break;
            //    case ConnectionType.AzureAccount:
            //        if (ci is CertificateBasedAuthenticationInfo)
            //        {
            //            displayName = "AzureSubscription," + (ci as CertificateBasedAuthenticationInfo).SubscriptionId;
            //        }
            //        else
            //        {
            //            displayName = "AzureSubscription";
            //        }
            //        break;
            //    case ConnectionType.Sql:
            //        displayName += "SQLServer";
            //        SqlConnectionInfo sqlCi = ci as SqlConnectionInfo;
            //        if (sqlCi.UseIntegratedSecurity == true)
            //        {
            //            displayName += ", trusted";
            //        }
            //        else
            //        {
            //            displayName += String.Format(System.Globalization.CultureInfo.InvariantCulture, ", user = {0}", sqlCi.UserName);
            //            //In Cloud a user can have access to only a few UDBs without access to master DB
            //            // and hence need to show different OE hierarchy trees for each DB
            //            //Same is the case with a contained user.


            //            if (ServerInfoCache.GetDatabaseEngineType(ci.ServerName) == DatabaseEngineType.SqlAzureDatabase
            //                || SFC.ExecuteSql.GetDatabaseEngineType(ci) == DatabaseEngineType.SqlAzureDatabase
            //                || SFC.ExecuteSql.IsContainedAuthentication(ci))
            //            {
            //                if (!string.IsNullOrEmpty(ci.DatabaseName))
            //                {
            //                    displayName += ", " + ci.DatabaseName;
            //                }
            //            }
            //        }
            //        break;
            //    case ConnectionType.Olap:
            //        displayName += "OLAP";
            //        break;
            //    case ConnectionType.SqlCE:
            //        displayName += "SqlCE";
            //        break;
            //    case ConnectionType.ReportServer:
            //        displayName += "Rs";
            //        displayName += String.Format(System.Globalization.CultureInfo.InvariantCulture, ", connection = {0}", ci.ConnectionString);
            //        break;
            //    case ConnectionType.IntegrationServer:
            //        displayName += "SSIS";
            //        break;
            //}
            //displayName += ")";
            //return displayName;
        }
    }
}
