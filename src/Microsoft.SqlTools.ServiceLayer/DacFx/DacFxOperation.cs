//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Extensions;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using static Microsoft.SqlTools.Utility.SqlConstants;
using Microsoft.SqlTools.Utility;
using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Base class for DacFx operations
    /// </summary>
    abstract class DacFxOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        private static Version serviceVersion = LoadServiceVersion();
        private const string TelemetryApplicationName = "sqltoolsservice";

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        public SqlTask SqlTask { get; set; }

        protected string ConnectionString { get; private set; }

        protected DacServices DacServices { get; private set; }

        protected bool TelemetryEnabled { get; private set; }

        protected ConnectionInfo ConnInfo { get; private set; }

        protected DacFxOperation(ConnectionInfo connInfo, bool telemetryEnabled = false)
        {
            Validate.IsNotNull("connectionInfo", connInfo);
            Validate.IsNotNull("connectionDetails", connInfo.ConnectionDetails);
            this.ConnInfo = connInfo;
            this.ConnectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
            this.OperationId = Guid.NewGuid().ToString();
            this.TelemetryEnabled = telemetryEnabled;
        }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error occurred during operation
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Cancel operation
        /// </summary>
        public void Cancel()
        {
            if (!this.cancellation.IsCancellationRequested)
            {
                Logger.Write(TraceEventType.Verbose, string.Format("Cancel invoked for OperationId {0}", this.OperationId));
                this.cancellation.Cancel();
            }
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

        public void Execute(TaskExecutionMode mode)
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                // Pass in Azure authentication token if needed
                this.DacServices = this.ConnInfo.ConnectionDetails.AzureAccountToken != null && this.ConnInfo.ConnectionDetails.AuthenticationType == AzureMFA
                    ? new DacServices(this.ConnectionString, new AccessTokenProvider(this.ConnInfo.ConnectionDetails.AzureAccountToken)) 
                    : new DacServices(this.ConnectionString);
                
                UpdateTelemetryStatus();
                Execute();
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, string.Format("DacFx import operation {0} failed with exception {1}", this.OperationId, e));
                throw;
            }
        }

        public abstract void Execute();

        protected DacDeployOptions GetDefaultDeployOptions()
        {
            DacDeployOptions options = new DacDeployOptions
            {
                AllowDropBlockingAssemblies = true,
                AllowIncompatiblePlatform = true,
                BlockOnPossibleDataLoss = false,
                DropObjectsNotInSource = true,
                DropPermissionsNotInSource = true,
                DropRoleMembersNotInSource = true,
                IgnoreKeywordCasing = false,
                IgnoreSemicolonBetweenStatements = false,
                IgnoreWhitespace = false,
            };

            return options;
        }

        /// <summary>
        /// Changes telemetry status 
        /// </summary>
        private void UpdateTelemetryStatus()
        {
            try
            {
                DacExtensions.SetServiceSetting(this.DacServices, "TelemetryEnabled", TelemetryEnabled);
                DacExtensions.SetServiceSetting(this.DacServices, "TelemetryApplicationName", TelemetryApplicationName);
                DacExtensions.SetServiceSetting(this.DacServices, "TelemetryApplicationVersion", serviceVersion);
            }
            catch(Exception ex)
            {
                Logger.Write(TraceEventType.Warning, string.Format("Failed to enable DacFx telemetry", this.OperationId, ex));
            }
        }

        private static Version LoadServiceVersion()
        {
            string fileVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
            if (Version.TryParse(fileVersion, out Version version)) 
            {
                return version;
            }   
            return null;
        }
    }
}
