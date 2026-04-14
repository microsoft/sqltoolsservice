//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal sealed class CreateDatabaseOnSaveOperation : ITaskOperation
    {
        private readonly DatabaseHandler databaseHandler;
        private readonly DatabaseViewContext databaseContext;
        private readonly DatabaseInfo databaseInfo;

        internal CreateDatabaseOnSaveOperation(DatabaseHandler databaseHandler, DatabaseViewContext databaseContext, DatabaseInfo databaseInfo)
        {
            Validate.IsNotNull(nameof(databaseHandler), databaseHandler);
            Validate.IsNotNull(nameof(databaseContext), databaseContext);
            Validate.IsNotNull(nameof(databaseInfo), databaseInfo);

            this.databaseHandler = databaseHandler;
            this.databaseContext = databaseContext;
            this.databaseInfo = databaseInfo;
        }

        internal string TaskDescription => $"Create database '{databaseInfo.Name ?? databaseContext.Parameters.Database}'.";

        internal Exception ExecutionException { get; private set; }

        public string ErrorMessage { get; private set; }

        public SqlTask SqlTask { get; set; }

        public void Execute(TaskExecutionMode mode)
        {
            try
            {
                ReportDescriptiveProgress();
                databaseHandler.Save(databaseContext, databaseInfo).GetAwaiter().GetResult();
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
    }
}
