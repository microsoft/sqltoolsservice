//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Dac;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Class to represent an in-progress schema compare publish database changes operation
    /// </summary>
    class SchemaComparePublishDatabaseChangesOperation : SchemaComparePublishChangesOperation
    {
        public SchemaComparePublishDatabaseChangesParams Parameters { get; }

        public SchemaComparePublishResult PublishResult { get; set; }

        public SchemaComparePublishDatabaseChangesOperation(SchemaComparePublishDatabaseChangesParams parameters, SchemaComparisonResult comparisonResult) : base(comparisonResult)
        {
            Validate.IsNotNull(nameof(parameters), parameters);
            Parameters = parameters;
        }

        public override void Execute(TaskExecutionMode mode)
        {
            CancellationToken.ThrowIfCancellationRequested();

            try
            {
                PublishResult = ComparisonResult.PublishChangesToDatabase(CancellationToken);

                if (!PublishResult.Success)
                {
                    // Sending only errors and warnings - because overall message might be too big for task view
                    ErrorMessage = String.Join(Environment.NewLine, this.PublishResult.Errors.Where(x => x.MessageType == SqlServer.Dac.DacMessageType.Error || x.MessageType == SqlServer.Dac.DacMessageType.Warning));
                    throw new DacServicesException(ErrorMessage);
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
