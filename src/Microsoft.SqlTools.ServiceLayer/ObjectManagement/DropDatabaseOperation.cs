//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal sealed class DropDatabaseOperation : IScriptableTaskOperation
    {
        internal static string TaskName => SR.DropDatabaseTaskName;

        private readonly DatabaseHandler databaseHandler;
        private readonly DropDatabaseRequestParams requestParams;

        internal DropDatabaseOperation(DatabaseHandler databaseHandler, DropDatabaseRequestParams requestParams)
        {
            Validate.IsNotNull(nameof(databaseHandler), databaseHandler);
            Validate.IsNotNull(nameof(requestParams), requestParams);

            this.databaseHandler = databaseHandler;
            this.requestParams = requestParams;
        }

        internal string TaskDescription => $"Drop database '{requestParams.Database}'.";

        public string ScriptContent { get; set; }

        public string ErrorMessage { get; private set; }

        public SqlTask SqlTask { get; set; }

        public void Execute(TaskExecutionMode mode)
        {
            try
            {
                ReportDescriptiveProgress();

                bool generateScript = mode == TaskExecutionMode.Script;
                var effectiveParams = new DropDatabaseRequestParams
                {
                    ConnectionUri = requestParams.ConnectionUri,
                    Database = requestParams.Database,
                    DropConnections = requestParams.DropConnections,
                    DeleteBackupHistory = requestParams.DeleteBackupHistory,
                    GenerateScript = generateScript,
                    Options = requestParams.Options
                };

                ScriptContent = databaseHandler.Drop(effectiveParams);
                if (!string.IsNullOrWhiteSpace(ScriptContent) && SqlTask != null)
                {
                    SqlTask.AddScript(SqlTaskStatus.Succeeded, ScriptContent);
                }
            }
            catch (Exception ex)
            {
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
    }
}