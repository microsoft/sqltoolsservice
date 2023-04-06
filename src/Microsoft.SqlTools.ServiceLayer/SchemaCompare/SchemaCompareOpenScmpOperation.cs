//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

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

        private XDocument scmpInfo { get; set; }

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
                SchemaComparison compare = new SchemaComparison(this.Parameters.FilePath);

                // load xml file because some parsing still needs to be done
                this.scmpInfo = XDocument.Load(this.Parameters.FilePath);

                this.Result = new SchemaCompareOpenScmpResult()
                {
                    DeploymentOptions = new DeploymentOptions(compare.Options),
                    Success = true,
                    SourceEndpointInfo = this.GetEndpointInfo(true, compare.Source),
                    TargetEndpointInfo = this.GetEndpointInfo(false, compare.Target),
                    OriginalTargetName = this.GetOriginalTargetName(),
                    OriginalTargetConnectionString = this.GetOriginalTargetConnectionString(),
                    ExcludedSourceElements = this.GetExcludedElements(compare.ExcludedSourceObjects),
                    ExcludedTargetElements = this.GetExcludedElements(compare.ExcludedTargetObjects)
                };
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format("Schema compare open scmp operation failed with exception {0}", e));
                throw;
            }
        }

        private SchemaCompareEndpointInfo GetEndpointInfo(bool source, SchemaCompareEndpoint endpoint)
        {
            SchemaCompareEndpointInfo endpointInfo = new SchemaCompareEndpointInfo();

            // if the endpoint is a dacpac, we don't need to parse the xml
            if (endpoint is SchemaCompareDacpacEndpoint dacpacEndpoint)
            {
                endpointInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                endpointInfo.PackageFilePath = dacpacEndpoint.FilePath;
            }
            else
            {
                bool isProjectEndpoint = endpoint is SchemaCompareProjectEndpoint;
                IEnumerable<XElement> result = isProjectEndpoint ? this.scmpInfo.Descendants("ProjectBasedModelProvider"): this.scmpInfo.Descendants("ConnectionBasedModelProvider");
                string searchingFor = source ? "Source" : "Target";
                
                // need to parse xml
                try
                {
                    if (result != null)
                    {
                        foreach (XElement node in result)
                        {
                            if (node.Parent.Name.ToString().Contains(searchingFor))
                            {
                                if(isProjectEndpoint)
                                {
                                    SetProjectEndpointInfoFromXML(result, endpointInfo, ((SchemaCompareProjectEndpoint)endpoint).ProjectFilePath);
                                    break;
                                }
                                else
                                {
                                    SetDatabaseEndpointInfoFromXML(node, endpointInfo);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    string info = isProjectEndpoint ? ((SchemaCompareProjectEndpoint)endpoint).ProjectFilePath : ((SchemaCompareDatabaseEndpoint)endpoint).DatabaseName;
                    ErrorMessage = string.Format(SR.OpenScmpConnectionBasedModelParsingError, info, e.Message);
                    Logger.Write(TraceEventType.Error, string.Format("Schema compare open scmp operation failed during xml parsing with exception {0}", e.Message));
                    throw;
                }
            }

            return endpointInfo;
        }

        private void SetDatabaseEndpointInfoFromXML(XElement node, SchemaCompareEndpointInfo endpointInfo)
        {
            // get connection string of database
            endpointInfo.ConnectionDetails = SchemaCompareService.ConnectionServiceInstance.ParseConnectionString(node.Value);
            endpointInfo.ConnectionDetails.ConnectionString = node.Value;
            endpointInfo.DatabaseName = endpointInfo.ConnectionDetails.DatabaseName;
            endpointInfo.EndpointType = SchemaCompareEndpointType.Database;
        }

        private void SetProjectEndpointInfoFromXML(IEnumerable<XElement> result, SchemaCompareEndpointInfo endpointInfo, string filePath)
        {
            // get dsp information
            IEnumerable<XElement> dsp = result.Descendants("Dsp");
            if (dsp != null)
            {
                endpointInfo.DataSchemaProvider = dsp.FirstOrDefault().Value;
            }

            // get folder structure information
            IEnumerable<XElement> fs = result.Descendants("FolderStructure");
            if (fs != null)
            {
                DacExtractTarget extractTarget;
                if (Enum.TryParse<DacExtractTarget>(fs.FirstOrDefault().Value, out extractTarget))
                {
                    endpointInfo.ExtractTarget = extractTarget;
                }
            }

            endpointInfo.EndpointType = SchemaCompareEndpointType.Project;
            endpointInfo.ProjectFilePath = filePath;
        }

        private List<SchemaCompareObjectId> GetExcludedElements(IList<SchemaComparisonExcludedObjectId> excludedObjects)
        {
            List<SchemaCompareObjectId> excludedElements = new List<SchemaCompareObjectId>();

            foreach (SchemaComparisonExcludedObjectId entry in excludedObjects)
            {
                excludedElements.Add(new SchemaCompareObjectId()
                {
                    NameParts = entry.Identifier.Parts.Cast<string>().ToArray(),
                    SqlObjectType = entry.TypeName
                });
            }

            return excludedElements;
        }


        // The original target name is used to determine whether to use ExcludedSourceElements or ExcludedTargetElements if source and target were swapped
        private string GetOriginalTargetName()
        {
            var result = this.scmpInfo.Descendants("PropertyElementName")
                .Where(x => x.Element("Name").Value == "TargetDatabaseName")
                .Select(x => x.Element("Value")).FirstOrDefault();

            return result != null ? result.Value : string.Empty;
        }

        // The original target connection string is used if comparing a dacpac and db with the same name
        private string GetOriginalTargetConnectionString()
        {
            var result = this.scmpInfo.Descendants("PropertyElementName")
              .Where(x => x.Element("Name").Value == "TargetConnectionString")
              .Select(x => x.Element("Value")).FirstOrDefault();

            return result != null ? result.Value : string.Empty;
        }
    }
}
