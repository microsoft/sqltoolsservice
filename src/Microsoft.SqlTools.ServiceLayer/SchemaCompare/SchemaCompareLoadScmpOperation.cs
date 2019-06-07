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
using System.Xml;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Schema compare load scmp operation
    /// </summary>
    class SchemaCompareLoadScmpOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        public SqlTask SqlTask { get; set; }

        public SchemaCompareLoadScmpParams Parameters { get; set; }

        public SchemaCompareLoadScmpResult Result { get; private set; }

        public SchemaCompareLoadScmpOperation(SchemaCompareLoadScmpParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
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
                SchemaComparison compare = new SchemaComparison(this.Parameters.filePath);

                this.Result = new SchemaCompareLoadScmpResult()
                {
                    DeploymentOptions = new DeploymentOptions(compare.Options),
                    Success = true
                };

                GetSourceAndTargetInfo();
                GetExcludedElements();
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare load scmp operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
        }

        private void GetSourceAndTargetInfo()
        {
            // parse source and target info from xml file
            XmlDocument scmpInfo = new XmlDocument();
            scmpInfo.Load(this.Parameters.filePath);

            XmlNodeList connectionBasedModelProviderNodes = scmpInfo.DocumentElement.SelectNodes("descendant::ConnectionBasedModelProvider");
            XmlNodeList fileBasedModelProviderNodes = scmpInfo.DocumentElement.SelectNodes("descendant::FileBasedModelProvider");

            this.Result.SourceEndpointInfo = new SchemaCompareEndpointInfo();
            this.Result.TargetEndpointInfo = new SchemaCompareEndpointInfo();

            foreach (XmlNode node in fileBasedModelProviderNodes)
            {
                if (node.ParentNode.Name.Contains("Source"))
                {
                    this.Result.SourceEndpointInfo.PackageFilePath = node.InnerText;
                    this.Result.SourceEndpointInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                }
                else
                {
                    this.Result.TargetEndpointInfo.PackageFilePath = node.InnerText;
                    this.Result.TargetEndpointInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                }
            }

            foreach (XmlNode node in connectionBasedModelProviderNodes)
            {
                if (node.ParentNode.Name.Contains("Source"))
                {
                    this.Result.SourceEndpointInfo.ConnectionDetails = SchemaCompareService.ConnectionServiceInstance.ParseConnectionString(node.InnerText);
                    this.Result.SourceEndpointInfo.ConnectionDetails.ConnectionString = node.InnerText;
                    this.Result.SourceEndpointInfo.EndpointType = SchemaCompareEndpointType.Database;
                }
                else
                {
                    this.Result.TargetEndpointInfo.ConnectionDetails = SchemaCompareService.ConnectionServiceInstance.ParseConnectionString(node.InnerText);
                    this.Result.TargetEndpointInfo.ConnectionDetails.ConnectionString = node.InnerText;
                    this.Result.TargetEndpointInfo.EndpointType = SchemaCompareEndpointType.Database;
                }
            }
        }

        private void GetExcludedElements()
        {
            // parse source and target info from xml file
            XmlDocument scmpInfo = new XmlDocument();
            scmpInfo.Load(this.Parameters.filePath);

            XmlNodeList excludedSourceNodes = scmpInfo.DocumentElement.SelectNodes("descendant::ExcludedSourceElements/SelectedItem");
            XmlNodeList excludedTargetNodes = scmpInfo.DocumentElement.SelectNodes("descendant::ExcludedTargetElements/SelectedItem");

            this.Result.ExcludedSourceElements = new List<string>();
            this.Result.ExcludedTargetElements = new List<string>();

            foreach (XmlNode node in excludedSourceNodes)
            {
                this.Result.ExcludedSourceElements.Add(GetConcatenatedElementName(node));
            }

            foreach (XmlNode node in excludedTargetNodes)
            {
                this.Result.ExcludedTargetElements.Add(GetConcatenatedElementName(node));
            }
        }

        // add unit test
        private string GetConcatenatedElementName(XmlNode node)
        {
            List<string> results = new List<string>();
            foreach (XmlNode n in node.ChildNodes)
            {
                results.Add(n.InnerText);
            }

            return String.Join(".", results);
        }
    }
}
