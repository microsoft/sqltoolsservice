//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.Utility;
using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    /// <summary>
    /// A task operation that needs full access to its database. Run it with
    /// <see cref="ExecuteWithFullDbAccessAsync"/>, which holds that access while it executes.
    /// </summary>
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
                lockedDatabaseManager ??= ConnectionService.Instance.LockedDatabaseManager;
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
        /// Full access to the database is taken asynchronously, so this operation must run through
        /// <see cref="ExecuteWithFullDbAccessAsync"/>. Running it directly would skip that access.
        /// </summary>
        public sealed override void Execute(TaskExecutionMode mode)
        {
            throw new InvalidOperationException($"{GetType().Name} needs full access to its database. Run it with {nameof(ExecuteWithFullDbAccessAsync)}.");
        }

        /// <summary>
        /// Settles what the operation runs against, such as its target database, before full
        /// access to that database is taken.
        /// </summary>
        protected virtual void PrepareToExecute()
        {
        }

        /// <summary>
        /// Executes the operation for the given mode while full access to its database is held
        /// </summary>
        protected virtual void ExecuteWhileHoldingAccess(TaskExecutionMode mode)
        {
            base.Execute(mode);
        }

        /// <summary>
        /// Prepares the operation, then executes it while holding full access to its database
        /// </summary>
        public async Task ExecuteWithFullDbAccessAsync(TaskExecutionMode mode)
        {
            // Preparation can change the target database, so it comes first: access must be
            // taken, and later released, on the database the operation actually uses.
            PrepareToExecute();

            try
            {
                await GainAccessToDatabaseAsync();
                ExecuteWhileHoldingAccess(mode);
            }
            catch (DatabaseFullAccessException)
            {
                Logger.Warning($"Failed to gain access to database. server|database:{ServerName}|{DatabaseName}");
                throw;
            }
            finally
            {
                // Released even when taking access failed partway, so connections it already
                // closed are reopened.
                await ReleaseAccessToDatabaseAsync();
            }
        }

        public async Task<bool> GainAccessToDatabaseAsync()
        {
            bool result = false;
            if (LockedDatabaseManager != null)
            {
                result = await LockedDatabaseManager.GainFullAccessToDatabaseAsync(ServerName, DatabaseName);
            }
            if(result && SourceDatabas != null &&  string.Compare(DatabaseName , SourceDatabas, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                result = await LockedDatabaseManager.GainFullAccessToDatabaseAsync(ServerName, SourceDatabas);
            }
            return result;
        }

        public async Task<bool> ReleaseAccessToDatabaseAsync()
        {
            bool result = false;
            if (LockedDatabaseManager != null)
            {
                result = await LockedDatabaseManager.ReleaseAccessAsync(ServerName, DatabaseName);
            }
            if (result && SourceDatabas != null && string.Compare(DatabaseName, SourceDatabas, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                result = await LockedDatabaseManager.ReleaseAccessAsync(ServerName, SourceDatabas);
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
