//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Class to represent an in-progress list objects operation.
    /// </summary>
    public sealed class ScriptingListObjectsOperation : ScriptingOperation
    {
        private bool disposed = false;

        public ScriptingListObjectsOperation(ScriptingListObjectsParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);

            this.Parameters = parameters;
        }

        private ScriptingListObjectsParams Parameters { get; set; }

        /// <summary>
        /// Event raised when a the list object operation is complete.
        /// </summary>
        /// <remarks>
        /// An event can be completed by the following conditions: success, cancel, error.
        /// </remarks>
        public event EventHandler<ScriptingListObjectsCompleteParams> CompleteNotification;

        public override void Execute()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            SqlScriptPublishModel publishModel = null;

            try
            {
                this.ValidateScriptDatabaseParams();

                publishModel = new SqlScriptPublishModel(this.Parameters.ConnectionString);
                List<ScriptingObject> databaseObjects = publishModel.GetDatabaseObjects();

                Logger.Write(
                    TraceEventType.Verbose,
                    string.Format(
                        "Sending list object completion notification count {0}, objects: {1}",
                        databaseObjects,
                        string.Join(", ", databaseObjects)));

                this.SendCompletionNotificationEvent(new ScriptingListObjectsCompleteParams
                {
                    OperationId = this.OperationId,
                    ScriptingObjects = databaseObjects,
                    Count = databaseObjects.Count,
                    Success = true,
                    SequenceNumber = 1,
                });
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Information, string.Format("Scripting operation {0} was canceled", this.OperationId));
                if (e.IsOperationCanceledException())
                {
                    this.SendCompletionNotificationEvent(new ScriptingListObjectsCompleteParams
                    {
                        OperationId = this.OperationId,
                        Canceled = true,
                    });
                }
                else
                {
                    Logger.Write(TraceEventType.Error, string.Format("Scripting operation {0} failed with exception {1}", this.OperationId, e));
                    this.SendCompletionNotificationEvent(new ScriptingListObjectsCompleteParams
                    {
                        OperationId = this.OperationId,
                        HasError = true,
                        ErrorMessage = e.Message,
                        ErrorDetails = e.ToString(),
                    });
                }
            }
        }

        private void SendCompletionNotificationEvent(ScriptingListObjectsCompleteParams parameters)
        {
            this.CompleteNotification?.Invoke(this, parameters);
        }

        private void ValidateScriptDatabaseParams()
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(this.Parameters.ConnectionString);
            }
            catch (Exception e)
            {
                throw new ArgumentException(SR.ScriptingListObjectsCompleteParams_ConnectionString_Property_Invalid, e);
            }
        }

        /// <summary>
        /// Disposes the scripting operation.
        /// </summary>
        public override void Dispose()
        {
            if (!disposed)
            {
                this.Cancel();
                disposed = true;
            }
        }
    }
}
