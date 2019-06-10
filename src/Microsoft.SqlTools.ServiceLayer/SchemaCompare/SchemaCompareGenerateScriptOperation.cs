//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Class to represent an in-progress schema compare generate script operation
    /// </summary>
    class SchemaCompareGenerateScriptOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        public SchemaCompareGenerateScriptParams Parameters { get; }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        public string ErrorMessage { get; set; }

        public SqlTask SqlTask { get; set; }

        public SchemaComparisonResult ComparisonResult { get; set; }

        public SchemaCompareScriptGenerationResult ScriptGenerationResult { get; set; }

        public SchemaCompareGenerateScriptOperation(SchemaCompareGenerateScriptParams parameters, SchemaComparisonResult comparisonResult)
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
                this.ScriptGenerationResult = this.ComparisonResult.GenerateScript(this.Parameters.TargetDatabaseName);

                // tests don't create a SqlTask, so only add the script when the SqlTask isn't null
                if (this.SqlTask != null)
                {
                    this.SqlTask.AddScript(SqlTaskStatus.Succeeded, this.ScriptGenerationResult.Script);
                    if (!string.IsNullOrEmpty(this.ScriptGenerationResult.MasterScript))
                    {
                        // master script is only used if the target is Azure SQL db and the script contains all operations that must be done against the master database
                        this.SqlTask.AddScript(SqlTaskStatus.Succeeded, ScriptGenerationResult.MasterScript);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare generate script operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
        }

        // The schema compare public api doesn't currently take a cancellation token so the operation can't be cancelled
        public void Cancel()
        {
        }

        /// <summary>
        /// Disposes the operation.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                this.Cancel();
                disposed = true;
            }
        }
    }
}
