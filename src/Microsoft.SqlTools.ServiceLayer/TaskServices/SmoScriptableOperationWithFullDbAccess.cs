//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.Utility;

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
            catch (DatabaseFullAccessException databaseFullAccessException)
            {
                Logger.Write(LogLevel.Warning, $"Failed to gain access to database. server|database:{ServerName}|{DatabaseName}");
                throw databaseFullAccessException;
            }
            catch
            {
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
            if (LockedDatabaseManager != null)
            {
                return LockedDatabaseManager.GainFullAccessToDatabase(ServerName, DatabaseName);
            }
            return false;
        }

        public bool ReleaseAccessToDatabase()
        {
            if (LockedDatabaseManager != null)
            {
                return LockedDatabaseManager.ReleaseAccess(ServerName, DatabaseName);
            }
            return false;
        }
    }
}
