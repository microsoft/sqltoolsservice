//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Class to represent an in-progress schema compare publish project changes operation
    /// </summary>
    class SchemaComparePublishProjectChangesOperation: ITaskOperation
    {
        public string OperationId { get; private set; }

        public SqlTask SqlTask { get; set; }

        public SchemaComparePublishProjectChangesParams Parameters { get; }

        public SchemaComparisonResult ComparisonResult { get; set; }

        public SchemaComparePublishProjectResult PublishResult { get; set; }

        public string ErrorMessage { get; set; }

        protected CancellationToken CancellationToken { get { return cancellation.Token; } }

        private readonly CancellationTokenSource cancellation = new();

        public SchemaComparePublishProjectChangesOperation(SchemaComparePublishProjectChangesParams parameters, SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull("parameters", parameters);
            Validate.IsNotNull("comparisonResult", comparisonResult);

            Parameters = parameters;
            ComparisonResult = comparisonResult;
        }

        public void Execute(TaskExecutionMode mode)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(CancellationToken);
            }

            try
            {
                PublishResult = ComparisonResult.PublishChangesToProject(Parameters.TargetProjectPath, Parameters.TargetFolderStructure);
                
                if (!PublishResult.Success)
                {
                    // Sending only errors and warnings - because overall message might be too big for task view
                    ErrorMessage = PublishResult.ErrorMessage;
                    throw new Exception(ErrorMessage);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare publish project changes operation {0} failed with exception {1}", OperationId, e.Message));
                throw;
            }
        }

        // The schema compare public api doesn't currently take a cancellation token so the operation can't be cancelled
        public void Cancel()
        {
            cancellation.Cancel();
        }
    }
}