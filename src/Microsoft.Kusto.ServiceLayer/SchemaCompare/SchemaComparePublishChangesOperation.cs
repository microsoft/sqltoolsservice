//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.Kusto.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.Kusto.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.Kusto.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Class to represent an in-progress schema compare publish changes operation
    /// </summary>
    class SchemaComparePublishChangesOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        public SchemaComparePublishChangesParams Parameters { get; }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        public string ErrorMessage { get; set; }

        public SqlTask SqlTask { get; set; }

        public SchemaComparisonResult ComparisonResult { get; set; }

        public SchemaComparePublishResult PublishResult { get; set; }

        public SchemaComparePublishChangesOperation(SchemaComparePublishChangesParams parameters, SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            Validate.IsNotNull("comparisonResult", comparisonResult);
            this.ComparisonResult = comparisonResult;
        }

        public void Execute(TaskExecutionMode mode)
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                this.PublishResult = this.ComparisonResult.PublishChangesToTarget(this.CancellationToken);
                if (!this.PublishResult.Success)
                {
                    // Sending only errors and warnings - because overall message might be too big for task view
                    ErrorMessage = string.Join(Environment.NewLine, this.PublishResult.Errors.Where(x => x.MessageType == SqlServer.Dac.DacMessageType.Error || x.MessageType == SqlServer.Dac.DacMessageType.Warning));
                    throw new Exception(ErrorMessage);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare publish changes operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
        }

        // The schema compare public api doesn't currently take a cancellation token so the operation can't be cancelled
        public void Cancel()
        {
            this.cancellation.Cancel();
        }
    }
}
