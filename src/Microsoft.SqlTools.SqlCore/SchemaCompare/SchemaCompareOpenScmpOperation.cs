//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.DacFx.Contracts;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare open SCMP operation.
    /// Connection string parsing is handled by ISchemaCompareConnectionProvider.
    /// </summary>
    public class SchemaCompareOpenScmpOperation : IDisposable
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;
        private readonly ISchemaCompareConnectionProvider _connectionProvider;

        /// <summary>
        /// Gets or sets the parameters for the open SCMP operation.
        /// </summary>
        public SchemaCompareOpenScmpParams Parameters { get; set; }

        /// <summary>
        /// The result of parsing the SCMP file, including endpoint info and deployment options.
        /// </summary>
        public SchemaCompareOpenScmpResult Result { get; private set; }

        private XDocument scmpInfo { get; set; }

        /// <summary>
        /// Initializes a new open SCMP operation with parameters and a connection provider.
        /// </summary>
        public SchemaCompareOpenScmpOperation(SchemaCompareOpenScmpParams parameters, ISchemaCompareConnectionProvider connectionProvider)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            this._connectionProvider = connectionProvider;
        }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// The error message if the operation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Cancels the running operation.
        /// </summary>
        public void Cancel()
        {
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

        /// <summary>
        /// Executes the SCMP file parsing and populates the result with source/target endpoint info and options.
        /// </summary>
        public void Execute()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                SchemaComparison compare = new SchemaComparison(this.Parameters.FilePath);

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
                Logger.Error(string.Format("Schema compare open scmp operation failed with exception {0}", e));
                throw;
            }
        }

        private SchemaCompareEndpointInfo GetEndpointInfo(bool source, SchemaCompareEndpoint endpoint)
        {
            SchemaCompareEndpointInfo endpointInfo = new SchemaCompareEndpointInfo();

            if (endpoint is SchemaCompareDacpacEndpoint dacpacEndpoint)
            {
                endpointInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                endpointInfo.PackageFilePath = dacpacEndpoint.FilePath;
            }
            else
            {
                bool isProjectEndpoint = endpoint is SchemaCompareProjectEndpoint;
                IEnumerable<XElement> result = isProjectEndpoint ? this.scmpInfo.Descendants("ProjectBasedModelProvider") : this.scmpInfo.Descendants("ConnectionBasedModelProvider");
                string searchingFor = source ? "Source" : "Target";

                try
                {
                    if (result != null)
                    {
                        foreach (XElement node in result)
                        {
                            if (node.Parent.Name.ToString().Contains(searchingFor))
                            {
                                if (isProjectEndpoint)
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
                    ErrorMessage = string.Format("Schema compare open scmp operation failed during xml parsing for '{0}': {1}", info, e.Message);
                    Logger.Error(string.Format("Schema compare open scmp operation failed during xml parsing with exception {0}", e.Message));
                    throw;
                }
            }

            return endpointInfo;
        }

        private void SetDatabaseEndpointInfoFromXML(XElement node, SchemaCompareEndpointInfo endpointInfo)
        {
            endpointInfo.ConnectionString = node.Value;
            if (_connectionProvider != null)
            {
                var parsed = _connectionProvider.ParseConnectionString(node.Value);
                endpointInfo.DatabaseName = parsed.DatabaseName;
                endpointInfo.ServerName = parsed.ServerName;
                endpointInfo.UserName = parsed.UserName;
            }
            else
            {
                // Fall back to SqlConnectionStringBuilder for parsing
                var builder = new SqlConnectionStringBuilder(node.Value);
                endpointInfo.DatabaseName = builder.InitialCatalog;
                endpointInfo.ServerName = builder.DataSource;
                endpointInfo.UserName = builder.UserID;
            }
            endpointInfo.EndpointType = SchemaCompareEndpointType.Database;
        }

        private void SetProjectEndpointInfoFromXML(IEnumerable<XElement> result, SchemaCompareEndpointInfo endpointInfo, string filePath)
        {
            IEnumerable<XElement> dsp = result.Descendants("Dsp");
            if (dsp != null)
            {
                endpointInfo.DataSchemaProvider = dsp.FirstOrDefault().Value;
            }

            IEnumerable<XElement> fs = result.Descendants("FolderStructure");
            if (fs != null)
            {
                DacExtractTarget extractTarget;
                if (fs.FirstOrDefault() != null)
                {
                    if (Enum.TryParse<DacExtractTarget>(fs.FirstOrDefault().Value, out extractTarget))
                    {
                        endpointInfo.ExtractTarget = extractTarget;
                    }
                    else
                    {
                        endpointInfo.ExtractTarget = DacExtractTarget.SchemaObjectType;
                        Logger.Error(string.Format("Schema compare open scmp operation failed during xml parsing with unknown ExtractTarget"));
                    }
                }
                else
                {
                    endpointInfo.ExtractTarget = DacExtractTarget.SchemaObjectType;
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

        private string GetOriginalTargetName()
        {
            var result = this.scmpInfo.Descendants("PropertyElementName")
                .Where(x => x.Element("Name").Value == "TargetDatabaseName")
                .Select(x => x.Element("Value")).FirstOrDefault();

            return result != null ? result.Value : string.Empty;
        }

        private string GetOriginalTargetConnectionString()
        {
            var result = this.scmpInfo.Descendants("PropertyElementName")
              .Where(x => x.Element("Name").Value == "TargetConnectionString")
              .Select(x => x.Element("Value")).FirstOrDefault();

            return result != null ? result.Value : string.Empty;
        }
    }
}
