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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Schema compare operation
    /// </summary>
    class SchemaCompareOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        public SqlTask SqlTask { get; set; }

        public SchemaCompareParams Parameters { get; set; }

        public string SourceConnectionString { get; set; }

        public string TargetConnectionString { get; set; }

        public SchemaComparisonResult ComparisonResult { get; set; }

        public List<DiffEntry> Differences;

        public SchemaCompareOperation(SchemaCompareParams parameters, ConnectionInfo sourceConnInfo, ConnectionInfo targetConnInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            this.SourceConnectionString = GetConnectionString(sourceConnInfo, parameters.SourceEndpointInfo.DatabaseName);
            this.TargetConnectionString = GetConnectionString(targetConnInfo, parameters.TargetEndpointInfo.DatabaseName);
            this.OperationId = Guid.NewGuid().ToString();
        }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error occurred during operation
        /// </summary>
        public string ErrorMessage { get; set; }

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

        public void Execute(TaskExecutionMode mode)
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                SchemaCompareEndpoint sourceEndpoint = CreateSchemaCompareEndpoint(this.Parameters.SourceEndpointInfo, this.SourceConnectionString);
                SchemaCompareEndpoint targetEndpoint = CreateSchemaCompareEndpoint(this.Parameters.TargetEndpointInfo, this.TargetConnectionString);

                SchemaComparison comparison = new SchemaComparison(sourceEndpoint, targetEndpoint);

                if (this.Parameters.DeploymentOptions != null)
                {
                    comparison.Options = this.CreateSchemaCompareOptions(this.Parameters.DeploymentOptions);
                }

                this.ComparisonResult = comparison.Compare();

                // try one more time if it didn't work the first time
                if (!this.ComparisonResult.IsValid)
                {
                    this.ComparisonResult = comparison.Compare();
                }

                this.Differences = new List<DiffEntry>();
                foreach (SchemaDifference difference in this.ComparisonResult.Differences)
                {
                    DiffEntry diffEntry = CreateDiffEntry(difference, null);
                    this.Differences.Add(diffEntry);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
        }

        private DacDeployOptions CreateSchemaCompareOptions(DeploymentOptions deploymentOptions)
        {
            System.Reflection.PropertyInfo[] deploymentOptionsProperties = deploymentOptions.GetType().GetProperties();

            DacDeployOptions dacOptions = new DacDeployOptions();
            foreach (var deployOptionsProp in deploymentOptionsProperties)
            {
                var prop = dacOptions.GetType().GetProperty(deployOptionsProp.Name);
                if (prop != null)
                {
                    prop.SetValue(dacOptions, deployOptionsProp.GetValue(deploymentOptions));
                }
            }
            return dacOptions;
        }

        internal static DiffEntry CreateDiffEntry(SchemaDifference difference, DiffEntry parent)
        {
            if(difference == null)
            {
                return null;
            }

            DiffEntry diffEntry = new DiffEntry();
            diffEntry.UpdateAction = difference.UpdateAction;
            diffEntry.DifferenceType = difference.DifferenceType;
            diffEntry.Name = difference.Name;

            if (difference.SourceObject != null)
            {
                diffEntry.SourceValue = GetName(difference.SourceObject.Name.ToString());
            }
            if (difference.TargetObject != null)
            {
                diffEntry.TargetValue = GetName(difference.TargetObject.Name.ToString());
            }

            if (difference.DifferenceType == SchemaDifferenceType.Object)
            {
                // set source and target scripts
                if (difference.SourceObject != null)
                {
                    string sourceScript;
                    difference.SourceObject.TryGetScript(out sourceScript);
                    diffEntry.SourceScript = RemoveExcessWhitespace(sourceScript);
                }
                if (difference.TargetObject != null)
                {
                    string targetScript;
                    difference.TargetObject.TryGetScript(out targetScript);
                    diffEntry.TargetScript = RemoveExcessWhitespace(targetScript);
                }
            }
            
            diffEntry.Children = new List<DiffEntry>();

            foreach (SchemaDifference child in difference.Children)
            {
                diffEntry.Children.Add(CreateDiffEntry(child, diffEntry));
            }

            return diffEntry;
        }

        private SchemaCompareEndpoint CreateSchemaCompareEndpoint(SchemaCompareEndpointInfo endpointInfo, string connectionString)
        {
            switch (endpointInfo.EndpointType)
            {
                case SchemaCompareEndpointType.Dacpac:
                    {
                        return new SchemaCompareDacpacEndpoint(endpointInfo.PackageFilePath);
                    }
                case SchemaCompareEndpointType.Database:
                    {
                        return new SchemaCompareDatabaseEndpoint(connectionString);
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        private string GetConnectionString(ConnectionInfo connInfo, string databaseName)
        {
            if (connInfo == null)
            {
                return null;
            }

            connInfo.ConnectionDetails.DatabaseName = databaseName;
            return ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
        }

        private static string RemoveExcessWhitespace(string script)
        {
            // replace all multiple spaces with single space
            return Regex.Replace(script, " {2,}", " ");
        }

        private static string GetName(string name)
        {
            // remove brackets from name
            return Regex.Replace(name, @"[\[\]]", "");
        }
    }
}
