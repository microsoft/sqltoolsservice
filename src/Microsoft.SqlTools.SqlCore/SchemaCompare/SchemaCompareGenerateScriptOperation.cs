//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Threading;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare generate script operation.
    /// Script delivery is handled by ISchemaCompareScriptHandler.
    /// </summary>
    public class SchemaCompareGenerateScriptOperation : IDisposable
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        /// <summary>
        /// Gets the parameters for the generate script operation.
        /// </summary>
        public SchemaCompareGenerateScriptParams Parameters { get; }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error message if the operation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The schema comparison result used to generate the script.
        /// </summary>
        public SchemaComparisonResult ComparisonResult { get; set; }

        /// <summary>
        /// The result of the script generation, containing the generated scripts.
        /// </summary>
        public SchemaCompareScriptGenerationResult ScriptGenerationResult { get; set; }

        /// <summary>
        /// Optional script handler for delivering generated scripts to the host.
        /// </summary>
        public ISchemaCompareScriptHandler ScriptHandler { get; set; }

        /// <summary>
        /// Initializes a new generate script operation with the given parameters, comparison result, and optional script handler.
        /// </summary>
        public SchemaCompareGenerateScriptOperation(SchemaCompareGenerateScriptParams parameters, SchemaComparisonResult comparisonResult, ISchemaCompareScriptHandler scriptHandler = null)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            Validate.IsNotNull("comparisonResult", comparisonResult);
            this.ComparisonResult = comparisonResult;
            this.ScriptHandler = scriptHandler;
        }

        /// <summary>
        /// Executes the script generation operation against the comparison result.
        /// </summary>
        public void Execute()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                this.ScriptGenerationResult = this.ComparisonResult.GenerateScript(this.Parameters.TargetDatabaseName, this.CancellationToken);

                if (this.ScriptHandler != null)
                {
                    this.ScriptHandler.OnScriptGenerated(this.ScriptGenerationResult.Script);
                    if (!string.IsNullOrEmpty(this.ScriptGenerationResult.MasterScript))
                    {
                        this.ScriptHandler.OnMasterScriptGenerated(this.ScriptGenerationResult.MasterScript);
                    }
                }

                if (!this.ScriptGenerationResult.Success)
                {
                    ErrorMessage = this.ScriptGenerationResult.Message;
                    throw new Exception(ErrorMessage);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Error(string.Format("Schema compare generate script operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
        }

        /// <summary>
        /// Cancels the running script generation operation.
        /// </summary>
        public void Cancel()
        {
            this.cancellation.Cancel();
        }

        /// <summary>
        /// Disposes the operation and cancels any pending work.
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
