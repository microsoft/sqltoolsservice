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
    /// Class to represent an in-progress schema compare publish database changes operation
    /// </summary>
    class SchemaComparePublishDatabaseChangesOperation : SchemaComparePublishChangesOperation
    {
        public SchemaComparePublishDatabaseChangesParams Parameters { get; }

        public SchemaComparePublishDatabaseResult PublishResult { get; set; }

        public SchemaComparePublishDatabaseChangesOperation(SchemaComparePublishDatabaseChangesParams parameters, SchemaComparisonResult comparisonResult) : base(comparisonResult)
        {
            Validate.IsNotNull("parameters", parameters);
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
                PublishResult = ComparisonResult.PublishChangesToDatabase(CancellationToken);
                if (!PublishResult.Success)
                {
                    // Sending only errors and warnings - because overall message might be too big for task view
                    ErrorMessage = string.Join(Environment.NewLine, this.PublishResult.Errors.Where(x => x.MessageType == SqlServer.Dac.DacMessageType.Error || x.MessageType == SqlServer.Dac.DacMessageType.Warning));
                    throw new Exception(ErrorMessage);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare publish database changes operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
        }
    }
}
