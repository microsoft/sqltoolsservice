//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    internal sealed class CreateDatabaseOperation : ITaskOperation
    {
        internal static string TaskName => SR.CreateDatabaseTaskName;

        private readonly ConnectionInfo connectionInfo;
        private readonly DatabaseInfo databaseInfo;

        internal CreateDatabaseOperation(ConnectionInfo connectionInfo, DatabaseInfo databaseInfo)
        {
            Validate.IsNotNull(nameof(connectionInfo), connectionInfo);
            Validate.IsNotNull(nameof(databaseInfo), databaseInfo);

            this.connectionInfo = connectionInfo;
            this.databaseInfo = databaseInfo;
            this.DatabaseName = GetDatabaseName(databaseInfo);
        }

        internal string DatabaseName { get; }

        internal string TaskDescription => $"Create database '{DatabaseName ?? connectionInfo.ConnectionDetails.DatabaseName}'.";

        internal Exception ExecutionException { get; private set; }

        public string ErrorMessage { get; private set; }

        public SqlTask SqlTask { get; set; }

        public void Execute(TaskExecutionMode mode)
        {
            try
            {
                ReportDescriptiveProgress();

                using (var taskHelper = AdminService.CreateDatabaseTaskHelper(connectionInfo))
                {
                    DatabaseTaskHelper.ApplyToPrototype(databaseInfo, taskHelper.Prototype);
                    taskHelper.Prototype.ApplyChanges();
                }
            }
            catch (Exception ex)
            {
                ExecutionException = ex;
                ErrorMessage = ex.Message;
                Logger.Error(ex);
                throw;
            }
        }

        public void Cancel()
        {
        }

        private void ReportDescriptiveProgress()
        {
            if (SqlTask != null && !string.IsNullOrWhiteSpace(TaskDescription))
            {
                SqlTask.ReportProgress(-1, TaskDescription);
            }
        }

        private static string GetDatabaseName(DatabaseInfo databaseInfo)
        {
            if (databaseInfo?.Options != null &&
                databaseInfo.Options.TryGetValue("name", out object value) &&
                value != null)
            {
                return value.ToString();
            }

            return null;
        }
    }
}