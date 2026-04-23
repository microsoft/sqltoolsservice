//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal sealed class SaveObjectOperation : ITaskOperation
    {
        private readonly IObjectTypeHandler objectTypeHandler;
        private readonly SqlObjectViewContext objectContext;
        private readonly SqlObject sqlObject;
        private readonly bool isCreateOperation;
        private readonly string objectDisplayName;

        internal SaveObjectOperation(IObjectTypeHandler objectTypeHandler, SqlObjectViewContext objectContext, SqlObject sqlObject)
        {
            Validate.IsNotNull(nameof(objectTypeHandler), objectTypeHandler);
            Validate.IsNotNull(nameof(objectContext), objectContext);
            Validate.IsNotNull(nameof(sqlObject), sqlObject);

            this.objectTypeHandler = objectTypeHandler;
            this.objectContext = objectContext;
            this.sqlObject = sqlObject;
            this.isCreateOperation = objectContext.Parameters.IsNewObject;
            this.objectDisplayName = string.IsNullOrWhiteSpace(sqlObject.Name)
                ? objectContext.Parameters.ObjectType.ToString()
                : sqlObject.Name;
        }

        internal string TaskName => isCreateOperation
            ? string.Format(CultureInfo.CurrentCulture, SR.SaveObjectCreateTaskName, objectDisplayName)
            : string.Format(CultureInfo.CurrentCulture, SR.SaveObjectUpdateTaskName, objectDisplayName);

        internal string TaskDescription => isCreateOperation
            ? string.Format(CultureInfo.CurrentCulture, SR.SaveObjectCreateTaskDescription, objectDisplayName)
            : string.Format(CultureInfo.CurrentCulture, SR.SaveObjectUpdateTaskDescription, objectDisplayName);

        internal string TargetDatabaseName => objectContext.Parameters.ObjectType == SqlObjectType.Database && objectContext.Parameters.IsNewObject
            ? sqlObject.Name
            : null;

        internal Exception ExecutionException { get; private set; }

        public string ErrorMessage { get; private set; }

        public SqlTask SqlTask { get; set; }

        public void Execute(TaskExecutionMode mode)
        {
            try
            {
                ReportDescriptiveProgress();
                objectTypeHandler.Save(objectContext, sqlObject).GetAwaiter().GetResult();
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