

//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.Utility;


namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{

    [Export(typeof(IHostedService))]
    public class SerializationService : HostedService<SerializationService>, IComposableService
    {
        private ConcurrentDictionary<string,DataSerializer> inProgressSerializations;

        public SerializationService()
        {
            inProgressSerializations = new ConcurrentDictionary<string,DataSerializer>();
        }


        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(TraceEventType.Verbose, "SerializationService initialized");
            serviceHost.SetRequestHandler(SerializeDataRequest.Type, HandleSerializeDataRequest);
        }

        /// <summary>
        /// Process request to save a resultSet to a file in CSV format
        /// </summary>
        internal async Task HandleSerializeDataRequest(SerializeDataRequestParams serializeParams,
            RequestContext<SerializeDataResult> requestContext)
        {
            try {
                Validate.IsNotNull(nameof(serializeParams), serializeParams);
                Validate.IsNotNullOrWhitespaceString("FilePath", serializeParams.FilePath);
                DataSerializer serializer = null;
                bool hasSerializer = inProgressSerializations.TryGetValue(serializeParams.FilePath, out serializer);
                if (!hasSerializer)
                {
                    serializer = new DataSerializer(serializeParams);
                    inProgressSerializations.AddOrUpdate(serializer.FilePath, serializer,  (key, old) => serializer);
                }
                Func<Task<SerializeDataResult>> writeData = () => {
                    return Task.Factory.StartNew(() => {
                        var result = serializer.ProcessRequest(serializeParams);
                        if (serializeParams.IsComplete)
                        {
                            // Cleanup the serializer
                            this.inProgressSerializations.TryRemove(serializer.FilePath, out serializer);
                        }
                        return result;
                    });
                };
                await HandleRequest(writeData, requestContext, "HandleSerializeDataRequest");
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.Message);
            }
        }

        private async Task HandleRequest<T>(Func<Task<T>> handler, RequestContext<T> requestContext, string requestType)
        {
            Logger.Write(TraceEventType.Verbose, requestType);

            try
            {
                T result = await handler();
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.Message);
            }
        }


    }

    public class DataSerializer
    {
        private IFileStreamWriter writer;
        private SerializeDataRequestParams requestParams;
        private IList<DbColumnWrapper> columns;

        public string FilePath { get; private set; }

        public DataSerializer(SerializeDataRequestParams requestParams)
        {
            this.requestParams = requestParams;
            this.columns = this.MapColumns(requestParams.Columns);
            this.FilePath = requestParams.FilePath;
        }

        private IList<DbColumnWrapper> MapColumns(ColumnInfo[] columns)
        {
            List<DbColumnWrapper> columnWrappers = new List<DbColumnWrapper>();
            foreach (ColumnInfo column in columns) {
                DbColumnWrapper wrapper = new DbColumnWrapper(column);
                columnWrappers.Add(wrapper);
            }
            return columnWrappers;
        }


        public SerializeDataResult ProcessRequest(SerializeDataRequestParams serializeParams)
        {
            SerializeDataResult result = new SerializeDataResult()
            {
            };
            try
            {
                this.WriteData(serializeParams.Rows, serializeParams.IsComplete);
                if (serializeParams.IsComplete)
                {
                    this.CloseStreams();
                }
                result.Succeeded = true;
            }
            catch (Exception ex)
            {
                result.Messages = ex.Message;
                result.Succeeded = false;
                this.CloseStreams();
            }
            return result;
        }

        public void WriteData(DbCellValue[][] rows, bool isComplete)
        {
            this.EnsureWriterCreated();
            foreach (var row in rows) {
                SetRawObjects(row);
                writer.WriteRow(row, this.columns);
            }
        }

        private void SetRawObjects(DbCellValue[] row)
        {
            for (int i = 0; i < row.Length; i++)
            {
                try
                {
                    // Try to set as the "correct" type
                    var value = Convert.ChangeType(row[i].DisplayValue, columns[i].DataType);
                    row[i].RawObject = value;
                }
                catch (Exception)
                {
                    row[i].RawObject = row[i].DisplayValue;
                }
            }
        }

        private void EnsureWriterCreated()
        {
            if (this.writer == null)
            {
                IFileStreamFactory factory;
                switch (this.requestParams.SaveFormat.ToLowerInvariant())
                {
                    case "json":
                        factory = new SaveAsJsonFileStreamFactory()
                        {
                            SaveRequestParams = CreateJsonRequestParams()
                        };
                        break;
                    case "csv":
                        factory = new SaveAsCsvFileStreamFactory()
                        {
                            SaveRequestParams = CreateCsvRequestParams()
                        };
                        break;
                    case "xml":
                        factory = new SaveAsXmlFileStreamFactory()
                        {
                            SaveRequestParams = CreateXmlRequestParams()
                        };
                        break;
                    case "excel":
                        factory = new SaveAsExcelFileStreamFactory()
                        {
                            SaveRequestParams = CreateExcelRequestParams()
                        };
                        break;
                    default:
                        throw new Exception("Unsupported Save Format: " + this.requestParams.SaveFormat);
                }
                this.writer = factory.GetWriter(requestParams.FilePath);
            }
        }
        private void CloseStreams()
        {
            this.writer.Dispose();
        }

        private SaveResultsAsJsonRequestParams CreateJsonRequestParams()
        {
            return new SaveResultsAsJsonRequestParams
            {
                FilePath = this.requestParams.FilePath,
                BatchIndex = 0,
                ResultSetIndex = 0
            };
        }
        private SaveResultsAsExcelRequestParams CreateExcelRequestParams()
        {
            return new SaveResultsAsExcelRequestParams
            {
                FilePath = this.requestParams.FilePath,
                BatchIndex = 0,
                ResultSetIndex = 0,
                IncludeHeaders = this.requestParams.IncludeHeaders
            };
        }
        private SaveResultsAsCsvRequestParams CreateCsvRequestParams()
        {
            return new SaveResultsAsCsvRequestParams
            {
                FilePath = this.requestParams.FilePath,
                BatchIndex = 0,
                ResultSetIndex = 0,
                IncludeHeaders = this.requestParams.IncludeHeaders,
                Delimiter = this.requestParams.Delimiter,
                LineSeperator = this.requestParams.LineSeparator,
                TextIdentifier = this.requestParams.TextIdentifier,
                Encoding = this.requestParams.Encoding
            };
        }
        private SaveResultsAsXmlRequestParams CreateXmlRequestParams()
        {
            return new SaveResultsAsXmlRequestParams
            {
                FilePath = this.requestParams.FilePath,
                BatchIndex = 0,
                ResultSetIndex = 0,
                Formatted = this.requestParams.Formatted,
                Encoding = this.requestParams.Encoding
            };
        }
    }

    class SerializationOptionsHelper
    {
        internal const string IncludeHeaders = "includeHeaders";
        internal const string Delimiter = "delimiter";
        internal const string LineSeparator = "lineSeparator";
        internal const string TextIdentifier = "textIdentifier";
        internal const string Encoding = "encoding";
        internal const string Formatted = "formatted";
    }
}