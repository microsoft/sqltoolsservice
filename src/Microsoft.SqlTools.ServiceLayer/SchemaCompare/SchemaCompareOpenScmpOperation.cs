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
    class SchemaCompareOpenScmpOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;

        public SqlTask SqlTask { get; set; }

        public SchemaCompareOpenScmpParams Parameters { get; set; }

        public SchemaCompareOpenScmpResult Result { get; private set; }

        private XmlDocument scmpInfo { get; set; }

        public SchemaCompareOpenScmpOperation(SchemaCompareOpenScmpParams parameters)
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

                // load xml file because some parsing still needs to be done
                this.scmpInfo = new XmlDocument();
                this.scmpInfo.Load(this.Parameters.filePath);

                this.Result = new SchemaCompareOpenScmpResult()
                {
                    DeploymentOptions = new DeploymentOptions(compare.Options),
                    Success = true,
                    SourceEndpointInfo = this.GetEndpointInfo(true),
                    TargetEndpointInfo = this.GetEndpointInfo(false),
                    OriginalTargetName = this.GetOriginalTargetName(),
                    OriginalTargetConnectionString = this.GetOriginalTargetConnectionString(),
                    ExcludedSourceElements = this.GetExcludedElements(true),
                    ExcludedTargetElements = this.GetExcludedElements(false)
                };
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare load scmp operation failed with exception {0}", e.Message));
                throw;
            }
        }

        private SchemaCompareEndpointInfo GetEndpointInfo(bool source)
        {
            XmlNodeList connectionBasedModelProviderNodes = this.scmpInfo.DocumentElement.SelectNodes("descendant::ConnectionBasedModelProvider");
            XmlNodeList fileBasedModelProviderNodes = this.scmpInfo.DocumentElement.SelectNodes("descendant::FileBasedModelProvider");

            SchemaCompareEndpointInfo endpointInfo = new SchemaCompareEndpointInfo();
            string searchingFor = source ? "Source" : "Target";

            foreach (XmlNode node in fileBasedModelProviderNodes)
            {
                if (node.ParentNode.Name.Contains(searchingFor))
                {
                    endpointInfo.PackageFilePath = node.InnerText;
                    endpointInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                }
            }

            foreach (XmlNode node in connectionBasedModelProviderNodes)
            {
                if (node.ParentNode.Name.Contains(searchingFor))
                {
                    endpointInfo.ConnectionDetails = SchemaCompareService.ConnectionServiceInstance.ParseConnectionString(node.InnerText);
                    endpointInfo.ConnectionDetails.ConnectionString = node.InnerText;
                    endpointInfo.DatabaseName = endpointInfo.ConnectionDetails.DatabaseName;
                    endpointInfo.EndpointType = SchemaCompareEndpointType.Database;
                }
            }

            return endpointInfo;
        }

        private List<string> GetExcludedElements(bool source)
        {
            XmlNodeList excludedNodes = source ? this.scmpInfo.DocumentElement.SelectNodes("descendant::ExcludedSourceElements/SelectedItem") : this.scmpInfo.DocumentElement.SelectNodes("descendant::ExcludedTargetElements/SelectedItem");
            List<string> excludedElements = new List<string>();

            foreach (XmlNode node in excludedNodes)
            {
                excludedElements.Add(GetConcatenatedElementName(node));
            }

            return excludedElements;
        }

        private string GetConcatenatedElementName(XmlNode node)
        {
            List<string> results = new List<string>();
            foreach (XmlNode n in node.ChildNodes)
            {
                results.Add(n.InnerText);
            }

            return string.Join(".", results);
        }

        // The original target name is used to determine whether to use ExcludedSourceElements or ExcludedTargetElements if source and target were swapped
        private string GetOriginalTargetName()
        {
            XmlNode node = this.scmpInfo.DocumentElement.SelectSingleNode("//PropertyElementName[Name='TargetDatabaseName']");
            return node != null ? node.LastChild.InnerText : string.Empty;
        }

        // The original target connection string is used if comparing a dacpac and db with the same name
        private string GetOriginalTargetConnectionString()
        {
            XmlNode node = this.scmpInfo.DocumentElement.SelectSingleNode("//PropertyElementName[Name='TargetConnectionString']");
            return node != null ? node.LastChild.InnerText : string.Empty;
        }
    }
}
