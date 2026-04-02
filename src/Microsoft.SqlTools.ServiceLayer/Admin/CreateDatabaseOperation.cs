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
        private readonly ConnectionInfo connectionInfo;
        private readonly DatabaseInfo databaseInfo;

        public CreateDatabaseOperation(ConnectionInfo connectionInfo, DatabaseInfo databaseInfo)
        {
            Validate.IsNotNull(nameof(connectionInfo), connectionInfo);
            Validate.IsNotNull(nameof(databaseInfo), databaseInfo);

            this.connectionInfo = connectionInfo;
            this.databaseInfo = databaseInfo;
        }

        public string ErrorMessage { get; private set; }

        public SqlTask SqlTask { get; set; }

        public void Execute(TaskExecutionMode mode)
        {
            try
            {
                using (var taskHelper = AdminService.CreateDatabaseTaskHelper(connectionInfo))
                {
                    DatabaseTaskHelper.ApplyToPrototype(databaseInfo, taskHelper.Prototype);
                    taskHelper.Prototype.ApplyChanges();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
        }

        public void Cancel()
        {
        }
    }
}