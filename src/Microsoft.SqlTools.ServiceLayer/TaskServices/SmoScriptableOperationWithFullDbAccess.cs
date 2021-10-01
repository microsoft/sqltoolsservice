﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.Utility;
using System;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    public abstract class SmoScriptableOperationWithFullDbAccess : SmoScriptableTaskOperation, IFeatureWithFullDbAccess
    {
        private DatabaseLocksManager lockedDatabaseManager;
        /// <summary>
        /// If an error occurred during task execution, this field contains the error message text
        /// </summary>
        public override abstract string ErrorMessage { get; }

        /// <summary>
        /// SMO Server instance used for the operation
        /// </summary>
        public override abstract Server Server { get; }
        public DatabaseLocksManager LockedDatabaseManager
        {
            get
            {
                if (lockedDatabaseManager == null)
                {
                    lockedDatabaseManager = ConnectionService.Instance.LockedDatabaseManager;
                }
                return lockedDatabaseManager;
            }
            set
            {
                lockedDatabaseManager = value;
            }
        }

        public abstract string ServerName { get; }

        public abstract string DatabaseName { get; }

        /// <summary>
        /// Cancels the operation
        /// </summary>
        public override abstract void Cancel();

        /// <summary>
        /// Executes the operations
        /// </summary>
        public override abstract void Execute();

        /// <summary>
        /// Execute the operation for given execution mode
        /// </summary>
        /// <param name="mode"></param>
        public override void Execute(TaskExecutionMode mode)
        {
            bool hasAccessToDb = false;
            try
            {
                hasAccessToDb = GainAccessToDatabase();
                base.Execute(mode);
            }
            catch (DatabaseFullAccessException)
            {
                Logger.Write(TraceEventType.Warning, $"Failed to gain access to database. server|database:{ServerName}|{DatabaseName}");
                throw;
            }
            finally
            {
                if (hasAccessToDb)
                {
                    ReleaseAccessToDatabase();
                }
            }
        }

        public bool GainAccessToDatabase()
        {
            bool result = false;
            if (LockedDatabaseManager != null)
            {
                result = LockedDatabaseManager.GainFullAccessToDatabase(ServerName, DatabaseName);
            }
            if(result && SourceDatabas != null &&  string.Compare(DatabaseName , SourceDatabas, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                result = LockedDatabaseManager.GainFullAccessToDatabase(ServerName, SourceDatabas);
            }
            return result;
        }

        public bool ReleaseAccessToDatabase()
        {
            bool result = false;
            if (LockedDatabaseManager != null)
            {
                result = LockedDatabaseManager.ReleaseAccess(ServerName, DatabaseName);
            }
            if (result && SourceDatabas != null && string.Compare(DatabaseName, SourceDatabas, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                result = LockedDatabaseManager.ReleaseAccess(ServerName, SourceDatabas);
            }
            return result;
        }

        private string SourceDatabas
        {
            get
            {
                return Server?.ConnectionContext.DatabaseName;
            }
        }
    }
}
