//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Threading;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare generate script operation
    /// </summary>
    public class SchemaCompareGenerateScriptOperation : IDisposable
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

        public SchemaComparisonResult ComparisonResult { get; set; }

        public SchemaCompareScriptGenerationResult ScriptGenerationResult { get; set; }

        /// <summary>
        /// Optional script handler for delivering generated scripts to the host.
        /// </summary>
        public ISchemaCompareScriptHandler ScriptHandler { get; set; }

        /// <summary>
        /// Raised for each DacFx message produced during script generation.
        /// Subscribe before calling <see cref="Execute"/> to receive notifications.
        /// </summary>
        public event EventHandler<SchemaCompareMessageEventArgs> Message;

        public SchemaCompareGenerateScriptOperation(SchemaCompareGenerateScriptParams parameters, SchemaComparisonResult comparisonResult, ISchemaCompareScriptHandler scriptHandler = null)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            Validate.IsNotNull("comparisonResult", comparisonResult);
            this.ComparisonResult = comparisonResult;
            this.ScriptHandler = scriptHandler;
            this.OperationId = !string.IsNullOrEmpty(parameters.OperationId) ? parameters.OperationId : Guid.NewGuid().ToString();
        }

        public void Execute()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                this.ScriptGenerationResult = this.ComparisonResult.GenerateScript(this.Parameters.TargetDatabaseName, this.CancellationToken);

                // Raise Message event with the script generation result message
                if (!string.IsNullOrEmpty(this.ScriptGenerationResult.Message))
                {
                    var msgType = this.ScriptGenerationResult.Success ? DacMessageType.Message : DacMessageType.Error;
                    Message?.Invoke(this, new SchemaCompareMessageEventArgs(
                        new DacMessage(msgType, 0, this.ScriptGenerationResult.Message, string.Empty, string.Empty)));
                }

                // deliver scripts to the host via the handler
                if (this.ScriptHandler != null)
                {
                    this.ScriptHandler.OnScriptGenerated(this.ScriptGenerationResult.Script);
                    if (!string.IsNullOrEmpty(this.ScriptGenerationResult.MasterScript))
                    {
                        // master script is only used if the target is Azure SQL db and the script contains all operations that must be done against the master database
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

        public void Cancel()
        {
            this.cancellation.Cancel();
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
