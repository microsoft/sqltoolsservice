//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public class DatabaseUtils
    {
        /// <summary>
        /// Check if the database is a system database
        /// </summary>
        /// <param name="databaseName">the name of database</param>
        /// <returns>return true if the database is a system database</returns>
        public static bool IsSystemDatabaseConnection(string databaseName)
        {
            return (string.IsNullOrWhiteSpace(databaseName) ||
                string.Compare(databaseName, CommonConstants.MasterDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.MsdbDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.ModelDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.TempDbDatabaseName, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public static string AddStringParameterForInsert(string paramValue)
        {
            string value = string.IsNullOrWhiteSpace(paramValue) ? paramValue : CUtils.EscapeStringSQuote(paramValue);
            return $"'{value}'";
        }

        public static string AddStringParameterForUpdate(string columnName, string paramValue)
        {
            string value = string.IsNullOrWhiteSpace(paramValue) ? paramValue : CUtils.EscapeStringSQuote(paramValue);
            return $"{columnName} = N'{value}'";
        }

        public static string AddByteArrayParameterForUpdate(string columnName, string paramName, string fileName, Dictionary<string, object> parameters)
        {
            byte[] contentBytes;
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    contentBytes = reader.ReadBytes((int)stream.Length);
                }
            }
            parameters.Add($"{paramName}", contentBytes);
            return $"{columnName} = @{paramName}";
        }

        public static string AddByteArrayParameterForInsert(string paramName, string fileName, Dictionary<string, object> parameters)
        {
            byte[] contentBytes;
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    contentBytes = reader.ReadBytes((int)stream.Length);
                }
            }
            parameters.Add($"{paramName}", contentBytes);
            return $"@{paramName}";
        }

        public static SecureString GetReadOnlySecureString(string secret)
        {
            SecureString ss = new SecureString();
            foreach (char c in secret.ToCharArray())
            {
                ss.AppendChar(c);
            }
            ss.MakeReadOnly();

            return ss;
        }

        public static bool IsSecureStringsEqual(SecureString ss1, SecureString ss2)
        {
            IntPtr bstr1 = IntPtr.Zero;
            IntPtr bstr2 = IntPtr.Zero;
            try
            {
                bstr1 = Marshal.SecureStringToBSTR(ss1);
                bstr2 = Marshal.SecureStringToBSTR(ss2);
                int length1 = Marshal.ReadInt32(bstr1, -4);
                int length2 = Marshal.ReadInt32(bstr2, -4);
                if (length1 == length2)
                {
                    for (int x = 0; x < length1; ++x)
                    {
                        byte b1 = Marshal.ReadByte(bstr1, x);
                        byte b2 = Marshal.ReadByte(bstr2, x);
                        if (b1 != b2) return false;
                    }
                }
                else return false;
                return true;
            }
            finally
            {
                if (bstr2 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr2);
                if (bstr1 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr1);
            }
        }

        /// <summary>
        /// this is the main method that is called by DropAllObjects for every object
        /// in the grid
        /// </summary>
        /// <param name="objectRowNumber"></param>
        public static void DoDropObject(CDataContainer dataContainer)
        {
            // if a server isn't connected then there is nothing to do      
            if (dataContainer.Server == null)
            {
                return;
            }

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

            Urn objUrn = sqlDialogSubject?.Urn;
            System.Diagnostics.Debug.Assert(objUrn != null);

            SfcObjectQuery objectQuery = new SfcObjectQuery(dataContainer.Server);

            IDroppable droppableObj = null;
            string[] fields = null;

            foreach (object obj in objectQuery.ExecuteIterator(new SfcQueryExpression(objUrn.ToString()), fields, null))
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
                objectName ??= string.Empty;
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

        private static void SpecialPreDropActionsForObject(CDataContainer dataContainer, IDroppable droppableObj,
            bool deleteBackupRestoreOrDisableAuditSpecOrDisableAudit, bool dropOpenConnections)
        {
            // if a server isn't connected then there is nothing to do      
            if (dataContainer.Server == null)
            {
                return;
            }

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
                if (db?.ActiveConnections > 0)
                {
                    // force the database to be single user
                    db.DatabaseOptions.UserAccess = DatabaseUserAccess.Single;
                    db.Alter(TerminationClause.RollbackTransactionsImmediately);
                }
            }
        }

        private static void SpecialPostDropActionsForObject(CDataContainer dataContainer, IDroppable droppableObj)
        {
            // if a server isn't connected then there is nothing to do      
            if (dataContainer.Server == null)
            {
                return;
            }

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

        public static string[] LoadSqlLogins(ServerConnection serverConnection)
        {
            return LoadItems(serverConnection, "Server/Login");
        }

        public static string[] LoadItems(ServerConnection serverConnection, string urn)
        {
            List<string> items = new List<string>();
            Request req = new Request();
            req.Urn = urn;
            req.ResultType = ResultType.IDataReader;
            req.Fields = new string[] { "Name" };

            Enumerator en = new Enumerator();
            using (IDataReader reader = en.Process(serverConnection, req).Data as IDataReader)
            {
                if (reader != null)
                {
                    string name;
                    while (reader.Read())
                    {
                        // Get the permission name
                        name = reader.GetString(0);
                        items.Add(name);
                    }
                }
            }
            items.Sort();
            return items.ToArray();
        }
    }
}
