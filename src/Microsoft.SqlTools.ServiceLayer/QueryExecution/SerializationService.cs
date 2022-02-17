//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{

    [Export(typeof(IHostedService))]
    public class SerializationService : HostedService<SerializationService>
    {
        private readonly ConcurrentDictionary<string, DataSerializer> inProgressSerializations;

        public SerializationService()
        {
            inProgressSerializations = new ConcurrentDictionary<string, DataSerializer>();
        }

        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(TraceEventType.Verbose, "SerializationService initialized");
            serviceHost.SetRequestHandler(SerializeStartRequest.Type, HandleSerializeStartRequest);
            serviceHost.SetRequestHandler(SerializeContinueRequest.Type, HandleSerializeContinueRequest);
        }

        /// <summary>
        /// Begin to process request to save a resultSet to a file in CSV format
        /// </summary>
        internal Task HandleSerializeStartRequest(
            SerializeDataStartRequestParams serializeParams,
            RequestContext<SerializeDataResult> requestContext)
        {
            // Run in separate thread so that message thread isn't held up by a potentially time consuming file write
            Task.Run(async () => {
                await RunSerializeStartRequest(serializeParams, requestContext);
            }).ContinueWithOnFaulted(async t => await SendErrorAndCleanup(serializeParams?.FilePath, requestContext, t.Exception));
            return Task.CompletedTask;
        }

        internal async Task RunSerializeStartRequest(SerializeDataStartRequestParams serializeParams, RequestContext<SerializeDataResult> requestContext)
        {
            try
            {
                // Verify we have sensible inputs and there isn't a task running for this file already
                Validate.IsNotNull(nameof(serializeParams), serializeParams);
                Validate.IsNotNullOrWhitespaceString("FilePath", serializeParams.FilePath);

                DataSerializer serializer = null;
                if (inProgressSerializations.TryGetValue(serializeParams.FilePath, out serializer))
                {
                    // Cannot proceed as there is an in progress serialization happening
                    throw new Exception(SR.SerializationServiceRequestInProgress(serializeParams.FilePath));
                }

                // Create a new serializer, save for future calls if needed, and write the request out
                serializer = new DataSerializer(serializeParams);
                if (!serializeParams.IsLastBatch)
                {
                    inProgressSerializations.AddOrUpdate(serializer.FilePath, serializer, (key, old) => serializer);
                }

                Logger.Write(TraceEventType.Verbose, "HandleSerializeStartRequest");
                SerializeDataResult result = serializer.ProcessRequest(serializeParams);
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await SendErrorAndCleanup(serializeParams.FilePath, requestContext, ex);
            }
        }

        private async Task SendErrorAndCleanup(string filePath, RequestContext<SerializeDataResult> requestContext, Exception ex)
        {
            if (filePath != null)
            {
                try
                {
                    DataSerializer removed;
                    inProgressSerializations.TryRemove(filePath, out removed);
                    if (removed != null)
                    {
                        // Flush any contents to disk and remove the writer
                        removed.CloseStreams();
                    }
                }
                catch
                {
                    // Do not care if there was an error removing this, must always delete if something failed
                }
            }
            await requestContext.SendError(ex.Message);
        }

        /// <summary>
        /// Process request to save a resultSet to a file in CSV format
        /// </summary>
        internal Task HandleSerializeContinueRequest(
            SerializeDataContinueRequestParams serializeParams,
            RequestContext<SerializeDataResult> requestContext)
        {
            // Run in separate thread so that message thread isn't held up by a potentially time consuming file write
            Task.Run(async () =>
            {
                await RunSerializeContinueRequest(serializeParams, requestContext);
            }).ContinueWithOnFaulted(async t => await SendErrorAndCleanup(serializeParams?.FilePath, requestContext, t.Exception));
            return Task.CompletedTask;
        }

        internal async Task RunSerializeContinueRequest(SerializeDataContinueRequestParams serializeParams, RequestContext<SerializeDataResult> requestContext)
        {
            try
            {
                // Verify we have sensible inputs and some data has already been sent for the file
                Validate.IsNotNull(nameof(serializeParams), serializeParams);
                Validate.IsNotNullOrWhitespaceString("FilePath", serializeParams.FilePath);

                DataSerializer serializer = null;
                if (!inProgressSerializations.TryGetValue(serializeParams.FilePath, out serializer))
                {
                    throw new Exception(SR.SerializationServiceRequestNotFound(serializeParams.FilePath));
                }

                // Write to file and cleanup if needed
                Logger.Write(TraceEventType.Verbose, "HandleSerializeContinueRequest");
                SerializeDataResult result = serializer.ProcessRequest(serializeParams);
                if (serializeParams.IsLastBatch)
                {
                    // Cleanup the serializer
                    this.inProgressSerializations.TryRemove(serializer.FilePath, out serializer);
                }
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await SendErrorAndCleanup(serializeParams.FilePath, requestContext, ex);
            }
        }
    }

    internal class DataSerializer
    {
        private readonly SerializeDataStartRequestParams requestParams;
        private readonly IReadOnlyList<DbColumnWrapper> columns;
        private ISaveAsFileStreamWriter writer;

        public DataSerializer(SerializeDataStartRequestParams requestParams)
        {
            this.requestParams = requestParams;
            columns = requestParams.Columns.Select(column => new DbColumnWrapper(column)).ToList();
            FilePath = requestParams.FilePath;
        }

        public string FilePath { get; }

        public SerializeDataResult ProcessRequest(ISerializationParams serializeParams)
        {
            SerializeDataResult result = new SerializeDataResult();
            try
            {
                this.WriteData(serializeParams.Rows, serializeParams.IsLastBatch);
                if (serializeParams.IsLastBatch)
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
            EnsureWriterCreated();
            foreach (var row in rows)
            {
                SetRawObjects(row);
                writer.WriteRow(row);
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
            if (writer != null)
            {
                return;
            }

            ISaveAsFileStreamFactory factory;
            switch (requestParams.SaveFormat.ToLowerInvariant())
            {
                case "json":
                    factory = new SaveAsJsonFileStreamFactory(null, CreateJsonRequestParams(), GetFileStream);
                    break;
                case "csv":
                    factory = new SaveAsCsvFileStreamFactory(null, CreateCsvRequestParams(), GetFileStream);
                    break;
                case "xml":
                    factory = new SaveAsXmlFileStreamFactory(null, CreateXmlRequestParams(), GetFileStream);
                    break;
                case "excel":
                    factory = new SaveAsExcelFileStreamFactory(null, CreateExcelRequestParams(), GetFileStream);
                    break;
                default:
                    throw new Exception(SR.SerializationServiceUnsupportedFormat(requestParams.SaveFormat));
            }
            writer = factory.GetWriter(requestParams.FilePath, columns);
        }

        public void CloseStreams()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        private FileStream GetFileStream(string fileName, FileMode fileMode, FileAccess fileAccess, FileShare fileShare) =>
            new FileStream(fileName, fileMode, fileAccess, fileShare);

        private SaveResultsAsJsonRequestParams CreateJsonRequestParams()
        {
            return new SaveResultsAsJsonRequestParams
            {
                FilePath = requestParams.FilePath,
                BatchIndex = 0,
                ResultSetIndex = 0
            };
        }

        private SaveResultsAsExcelRequestParams CreateExcelRequestParams()
        {
            return new SaveResultsAsExcelRequestParams
            {
                FilePath = requestParams.FilePath,
                BatchIndex = 0,
                ResultSetIndex = 0,
                IncludeHeaders = this.requestParams.IncludeHeaders
            };
        }

        private SaveResultsAsCsvRequestParams CreateCsvRequestParams()
        {
            return new SaveResultsAsCsvRequestParams
            {
                FilePath = requestParams.FilePath,
                BatchIndex = 0,
                ResultSetIndex = 0,
                IncludeHeaders = requestParams.IncludeHeaders,
                Delimiter = requestParams.Delimiter,
                LineSeperator = requestParams.LineSeparator,
                TextIdentifier = requestParams.TextIdentifier,
                Encoding = requestParams.Encoding,
                MaxCharsToStore = requestParams.MaxCharsToStore
            };
        }

        private SaveResultsAsXmlRequestParams CreateXmlRequestParams()
        {
            return new SaveResultsAsXmlRequestParams
            {
                FilePath = requestParams.FilePath,
                BatchIndex = 0,
                ResultSetIndex = 0,
                Formatted = requestParams.Formatted,
                Encoding = requestParams.Encoding
            };
        }
    }
}