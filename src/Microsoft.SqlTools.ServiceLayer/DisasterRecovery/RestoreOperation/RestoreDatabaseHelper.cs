﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.Utility;
using System.Collections.Concurrent;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    /// <summary>
    /// Includes method to all restore operations
    /// </summary>
    public class RestoreDatabaseHelper
    {
        public const string LastBackupTaken = "lastBackupTaken";
        private ConcurrentDictionary<string, RestoreDatabaseTaskDataObject> sessions = new ConcurrentDictionary<string, RestoreDatabaseTaskDataObject>(); 

        /// <summary>
        /// Creates response which includes information about the server given to restore (default data location, db names with backupsets)
        /// </summary>
        public RestoreConfigInfoResponse CreateConfigInfoResponse(RestoreConfigInfoRequestParams restoreConfigInfoRequest)
        {
            RestoreConfigInfoResponse response = new RestoreConfigInfoResponse();
            RestoreDatabaseTaskDataObject restoreTaskObject = null;
            
            try
            {
                restoreTaskObject = CreateRestoreForNewSession(restoreConfigInfoRequest.OwnerUri);
                if (restoreTaskObject != null)
                {
                    // Default Data folder path in the target server
                    response.ConfigInfo.Add(RestoreOptionsHelper.DataFileFolder, restoreTaskObject.DefaultDataFileFolder);
                    // Default log folder path in the target server
                    response.ConfigInfo.Add(RestoreOptionsHelper.LogFileFolder, restoreTaskObject.DefaultLogFileFolder);
                    // The db names with backup set
                    response.ConfigInfo.Add(RestoreOptionsHelper.SourceDatabaseNamesWithBackupSets, restoreTaskObject.GetDatabaseNamesWithBackupSets());
                    // Default backup folder path in the target server
                    response.ConfigInfo.Add(RestoreOptionsHelper.DefaultBackupFolder, restoreTaskObject.DefaultBackupFolder);
                }
            }
            catch(Exception ex) 
            {
                Logger.Write(TraceEventType.Warning, $"Failed to create restore config info. error: { ex.Message}");
                response.ErrorMessage = ex.Message;
            }
            finally
            {
                ServerConnection serverConnection = restoreTaskObject?.Server?.ConnectionContext;
                if (serverConnection != null && serverConnection.IsOpen)
                {
                    restoreTaskObject.Server.ConnectionContext.Disconnect();
                }
            }
            return response;
            
        }

        /// <summary>
        /// Cancels existing restore plan
        /// </summary>
        public bool CancelRestorePlan(RestoreParams restoreParams)
        {
            RestoreDatabaseTaskDataObject restoreTaskObject = null;
            string sessionId = restoreParams.SessionId;
            if (!string.IsNullOrEmpty(sessionId) && sessions.TryGetValue(sessionId, out restoreTaskObject))
            {
                ServerConnection connection = restoreTaskObject?.Server?.ConnectionContext;
                if (connection != null && connection.IsOpen)
                {
                    connection.Disconnect();
                }
                sessions.TryRemove(sessionId, out restoreTaskObject);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a restore plan, The result includes the information about the backup set, 
        /// the files and the database to restore to
        /// </summary>
        /// <param name="requestParam">Restore request</param>s
        /// <returns>Restore plan</returns>
        public RestorePlanResponse CreateRestorePlanResponse(RestoreDatabaseTaskDataObject restoreDataObject)
        {
            RestorePlanResponse response = new RestorePlanResponse()
            {
                DatabaseName = restoreDataObject.RestoreParams.TargetDatabaseName,
                PlanDetails = new System.Collections.Generic.Dictionary<string, RestorePlanDetailInfo>()
            };
            try
            {
                if (restoreDataObject != null && restoreDataObject.IsValid)
                {
                    restoreDataObject.UpdateRestoreTaskObject();

                    if (restoreDataObject != null && restoreDataObject.IsValid)
                    {
                        response.SessionId = restoreDataObject.SessionId;
                        response.DatabaseName = restoreDataObject.TargetDatabaseName;

                        response.PlanDetails.Add(RestoreOptionsHelper.TargetDatabaseName, 
                            RestoreOptionFactory.Instance.CreateAndValidate(RestoreOptionsHelper.TargetDatabaseName, restoreDataObject));
                        response.PlanDetails.Add(RestoreOptionsHelper.SourceDatabaseName, 
                            RestoreOptionFactory.Instance.CreateAndValidate(RestoreOptionsHelper.SourceDatabaseName, restoreDataObject));

                        response.PlanDetails.Add(RestoreOptionsHelper.ReadHeaderFromMedia, RestorePlanDetailInfo.Create(
                           name: RestoreOptionsHelper.ReadHeaderFromMedia,
                           currentValue: restoreDataObject.RestorePlanner.ReadHeaderFromMedia));
                        response.DbFiles = restoreDataObject.DbFiles.Select(x => new RestoreDatabaseFileInfo
                        {
                            FileType = x.DbFileType,
                            LogicalFileName = x.LogicalName,
                            OriginalFileName = x.PhysicalName,
                            RestoreAsFileName = x.PhysicalNameRelocate
                        });
                        response.CanRestore = CanRestore(restoreDataObject);

                        response.PlanDetails.Add(LastBackupTaken, 
                            RestorePlanDetailInfo.Create(name: LastBackupTaken, currentValue: restoreDataObject.GetLastBackupTaken(), isReadOnly: true));

                        response.BackupSetsToRestore = restoreDataObject.GetSelectedBakupSets();
                        var dbNames = restoreDataObject.SourceDbNames;
                        response.DatabaseNamesFromBackupSets = dbNames == null ? new string[] { } : dbNames.ToArray();

                        RestoreOptionsHelper.AddOptions(response, restoreDataObject);
                      
                    }
                    else
                    {
                        if (restoreDataObject.ActiveException != null)
                        {
                            response.ErrorMessage = restoreDataObject.ActiveException.Message;
                        }
                        else
                        {
                            response.ErrorMessage = SR.RestorePlanFailed;
                        }
                        response.CanRestore = false;
                    }
                }
                else
                {
                    response.ErrorMessage = SR.RestorePlanFailed;
                }
            }
            catch(Exception ex)
            {
                response.ErrorMessage = ex.Message;

                if (ex.InnerException != null)
                {
                    response.ErrorMessage += Environment.NewLine;
                    response.ErrorMessage += ex.InnerException.Message;
                }
                Logger.Write(TraceEventType.Information, $"Failed to create restore plan. error: { response.ErrorMessage}");
            }
            return response;

        }

        /// <summary>
        /// Returns true if the restoring the restoreDataObject is supported in the service
        /// </summary>
        private static bool CanRestore(RestoreDatabaseTaskDataObject restoreDataObject)
        {
            return restoreDataObject != null && restoreDataObject.RestorePlan != null && restoreDataObject.RestorePlan.RestoreOperations != null
                && restoreDataObject.RestorePlan.RestoreOperations.Count > 0;
        }

        /// <summary>
        /// Creates anew restore task object to do the restore operations
        /// </summary>
        /// <param name="restoreParams">Restore request parameters</param>
        /// <returns>Restore task object</returns>
        public RestoreDatabaseTaskDataObject CreateRestoreDatabaseTaskDataObject(RestoreParams restoreParams, ConnectionInfo connectionInfo = null)
        {
            RestoreDatabaseTaskDataObject restoreTaskObject = null;
            string sessionId = string.IsNullOrWhiteSpace(restoreParams.SessionId) ? Guid.NewGuid().ToString() : restoreParams.SessionId;
            if (!sessions.TryGetValue(sessionId, out restoreTaskObject))
            {
                restoreTaskObject = CreateRestoreForNewSession(restoreParams.OwnerUri, restoreParams.TargetDatabaseName);
                sessions.AddOrUpdate(sessionId, restoreTaskObject, (key, old) => restoreTaskObject);
            }
            restoreTaskObject.SessionId = sessionId;
            restoreTaskObject.RestoreParams = restoreParams;
            if (connectionInfo != null)
            {
                restoreTaskObject.ConnectionInfo = connectionInfo;
            }
            
            return restoreTaskObject;
        }

        private RestoreDatabaseTaskDataObject CreateRestoreForNewSession(string ownerUri, string targetDatabaseName = null)
        {
            ConnectionInfo connInfo;
            DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                    ownerUri,
                    out connInfo);

            if (connInfo != null)
            {
                Server server = new Server(ConnectionService.OpenServerConnection(connInfo, "Restore"));

                RestoreDatabaseTaskDataObject restoreDataObject = new RestoreDatabaseTaskDataObject(server, targetDatabaseName);
                return restoreDataObject;
            }
            return null;
        }

       

        private bool CanChangeTargetDatabase(RestoreDatabaseTaskDataObject restoreDataObject)
        {
            return DatabaseUtils.IsSystemDatabaseConnection(restoreDataObject.Server.ConnectionContext.DatabaseName);
        }
    }
}
