//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal sealed class RenameObjectOperation : ITaskOperation
    {
        private readonly IObjectTypeHandler objectTypeHandler;
        private readonly RenameRequestParams requestParams;
        private readonly string taskName;
        private readonly string taskObjectType;

        internal RenameObjectOperation(IObjectTypeHandler objectTypeHandler, RenameRequestParams requestParams)
        {
            Validate.IsNotNull(nameof(objectTypeHandler), objectTypeHandler);
            Validate.IsNotNull(nameof(requestParams), requestParams);

            this.objectTypeHandler = objectTypeHandler;
            this.requestParams = requestParams;

            string objectName = requestParams.ObjectType.ToString();
            string objectType = requestParams.ObjectType.ToString();
            if (!string.IsNullOrWhiteSpace(requestParams.ObjectUrn))
            {
                try
                {
                    Urn urn = new Urn(requestParams.ObjectUrn);
                    string urnName = urn.GetAttribute("Name");
                    if (!string.IsNullOrWhiteSpace(urnName))
                    {
                        objectName = Urn.UnEscapeString(urnName);
                    }

                    if (!string.IsNullOrWhiteSpace(urn.Type))
                    {
                        objectType = urn.Type;
                    }
                }
                catch
                {
                }
            }

            this.taskName = string.Format(CultureInfo.CurrentCulture, SR.RenameTaskName, objectName);
            this.taskObjectType = objectType.ToLowerInvariant();
        }

        internal string TaskName => taskName;

        internal string TaskDescription => string.Format(CultureInfo.CurrentCulture, SR.RenameTaskDescription, taskObjectType, requestParams.NewName);

        internal string TargetDatabaseName => requestParams.ObjectType == SqlObjectType.Database ? requestParams.NewName : null;

        internal Exception ExecutionException { get; private set; }

        public string ErrorMessage { get; private set; }

        public SqlTask SqlTask { get; set; }

        public void Execute(TaskExecutionMode mode)
        {
            try
            {
                ReportDescriptiveProgress();
                objectTypeHandler.Rename(requestParams.ConnectionUri, requestParams.ObjectUrn, requestParams.NewName).GetAwaiter().GetResult();
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