//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
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
            this.SourceConnectionString = GetConnectionString(sourceConnInfo, parameters.sourceEndpointInfo.DatabaseName);
            this.TargetConnectionString = GetConnectionString(targetConnInfo, parameters.targetEndpointInfo.DatabaseName);
            this.OperationId = Guid.NewGuid().ToString();
        }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error occurred during operation
        /// </summary>
        public string ErrorMessage { get; set; }

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
                SchemaCompareEndpoint sourceEndpoint = CreateSchemaCompareEndpoint(this.Parameters.sourceEndpointInfo, this.SourceConnectionString);
                SchemaCompareEndpoint targetEndpoint = CreateSchemaCompareEndpoint(this.Parameters.targetEndpointInfo, this.TargetConnectionString);

                SchemaComparison comparison = new SchemaComparison(sourceEndpoint, targetEndpoint);
                this.ComparisonResult = comparison.Compare();

                // try one more time if it didn't work the first time
                if(!this.ComparisonResult.IsValid)
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
                Logger.Write(TraceEventType.Error, string.Format("Schema compare operation {0} failed with exception {1}", this.OperationId, e));
                throw;
            }
        }

        private DiffEntry CreateDiffEntry(SchemaDifference difference, DiffEntry parent)
        {
            DiffEntry diffEntry = new DiffEntry();
            diffEntry.updateAction = difference.UpdateAction;
            diffEntry.differenceType = difference.DifferenceType;
            diffEntry.name = difference.Name;

            if (difference.SourceObject != null)
            {
                diffEntry.sourceValue = GetName(difference.SourceObject.Name.ToString());
            }
            if (difference.TargetObject != null)
            {
                diffEntry.targetValue = GetName(difference.TargetObject.Name.ToString());
            }

            if (difference.DifferenceType == SchemaDifferenceType.Object)
            {
                // set source and target scripts
                if (difference.SourceObject != null)
                {
                    string sourceScript;
                    difference.SourceObject.TryGetScript(out sourceScript);
                    diffEntry.sourceScript = RemoveExcessWhitespace(sourceScript);
                }
                if (difference.TargetObject != null)
                {
                    string targetScript;
                    difference.TargetObject.TryGetScript(out targetScript);
                    diffEntry.targetScript = RemoveExcessWhitespace(targetScript);
                }
            }

            diffEntry.children = new List<DiffEntry>();

            foreach (SchemaDifference child in difference.Children)
            {
                diffEntry.children.Add(CreateDiffEntry(child, diffEntry));
            }

            return diffEntry;
        }

        private SchemaCompareEndpoint CreateSchemaCompareEndpoint(SchemaCompareEndpointInfo endpointInfo, string connectionString)
        {
            switch (endpointInfo.EndpointType)
            {
                case SchemaCompareEndpointType.dacpac:
                {
                    return new SchemaCompareDacpacEndpoint(endpointInfo.PackageFilePath);
                }
                case SchemaCompareEndpointType.database:
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

        private string RemoveExcessWhitespace(string script)
        {
            // replace all multiple spaces with single space
            return Regex.Replace(script, " {2,}", " ");
        }

        private string GetName(string name)
        {
            // remove brackets from name
            return Regex.Replace(name, @"[\[\]]", "");
        }
    }
}
