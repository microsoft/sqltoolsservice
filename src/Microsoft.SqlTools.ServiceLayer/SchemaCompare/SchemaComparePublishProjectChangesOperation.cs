//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Class to represent an in-progress schema compare publish project changes operation
    /// </summary>
    class SchemaComparePublishProjectChangesOperation: SchemaComparePublishChangesOperation
    {
        public SchemaComparePublishProjectChangesParams Parameters { get; }

        public SchemaComparePublishProjectResult PublishResult { get; set; }

        public SchemaComparePublishProjectChangesOperation(SchemaComparePublishProjectChangesParams parameters, SchemaComparisonResult comparisonResult) : base(comparisonResult)
        {
            Validate.IsNotNull(nameof(parameters), parameters);
            Parameters = parameters;
        }

        public override void Execute(TaskExecutionMode mode)
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
    }
}