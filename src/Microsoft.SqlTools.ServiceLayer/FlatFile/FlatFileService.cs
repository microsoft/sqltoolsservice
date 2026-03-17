//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.FlatFile.Contracts;
using Microsoft.SqlTools.Utility;
using Prose = Microsoft.SqlServer.Prose.Import;

namespace Microsoft.SqlTools.ServiceLayer.FlatFile
{
    /// <summary>
    /// Service that provides flat file preview and import operations.
    /// </summary>
    public class FlatFileService
    {
        private static readonly Lazy<FlatFileService> instance = new Lazy<FlatFileService>(() => new FlatFileService());

        private readonly ConcurrentDictionary<string, FlatFileSession> sessions = new ConcurrentDictionary<string, FlatFileSession>();

        internal FlatFileService()
        {
        }

        public static FlatFileService Instance => instance.Value;

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Verbose("FlatFile service initialized");

            serviceHost.SetRequestHandler(ProseDiscoveryRequest.Type, HandleProseDiscoveryRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(InsertDataRequest.Type, HandleInsertDataRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetColumnInfoRequest.Type, HandleGetColumnInfoRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(ChangeColumnSettingsRequest.Type, HandleChangeColumnSettingsRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(LearnTransformationRequest.Type, HandleLearnTransformationRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(SaveTransformationRequest.Type, HandleSaveTransformationRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(DisposeSessionRequest.Type, HandleDisposeSessionRequest, isParallelProcessingSupported: true);
        }

        internal async Task HandleProseDiscoveryRequest(
            ProseDiscoveryParams parameters,
            RequestContext<ProseDiscoveryResponse> requestContext)
        {
            Logger.Verbose(nameof(HandleProseDiscoveryRequest));

            try
            {
                Validate.IsNotNull(nameof(parameters), parameters);
                Validate.IsNotNull(nameof(requestContext), requestContext);
                ValidateOperationId(parameters.OperationId);

                ProseDiscoveryResponse response = await Task.Run(() =>
                {
                    string tempFilePath = null;
                    try
                    {
                        Prose.BcpProcessType processType = GetProcessType(parameters.FileType);

                        // Use fileContents instead of filePath when available, since the source path
                        // may not be directly readable by the service process.
                        if (parameters.FileContents != null)
                        {
                            tempFilePath = Path.GetTempFileName();
                            File.WriteAllText(tempFilePath, parameters.FileContents);
                        }

                        var options = new Prose.BcpProcessOptions(false, processType);
                        var process = new Prose.BcpProcess(
                            tempFilePath ?? parameters.FilePath,
                            parameters.TableName,
                            parameters.SchemaName,
                            options);

                        EnsureSuccess(process.Learn());
                        EnsureSuccess(process.GenerateDataPreview());
                        ReplaceSession(parameters.OperationId, new FlatFileSession(process));

                        return CreateDiscoveryResponse(process);
                    }
                    finally
                    {
                        if (tempFilePath != null && File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }
                    }
                });

                await requestContext.SendResult(response);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        internal async Task HandleInsertDataRequest(
            InsertDataParams parameters,
            RequestContext<InsertDataResponse> requestContext)
        {
            Logger.Verbose("Handling flatfile insert data request");

            bool success = true;
            string errorMessage = null;

            try
            {
                var result = await ExecuteSessionOperation(
                    parameters.OperationId,
                    async session =>
                    {
                        var process = session.Process;
                        var createTableResult = process.GenerateCreateTableQueryText(process.ColumnInfos);
                        if (!createTableResult.Success)
                        {
                            return createTableResult;
                        }

                        string connectionString = ResolveImportConnectionString(parameters);
                        return await Task.Run(() => process.CreateTableAndInsertDataIntoDb(
                            connectionString,
                            parameters.BatchSize,
                            null));
                    });
                if (!result.Success)
                {
                    success = false;
                    errorMessage = GetErrorMessage(result);
                }
                else
                {
                    RemoveSession(parameters.OperationId);
                }
            }
            catch (Exception ex)
            {
                success = false;
                errorMessage = ex.ToString();
            }

            await requestContext.SendResult(new InsertDataResponse
            {
                Result = new Result
                {
                    Success = success,
                    ErrorMessage = errorMessage
                }
            });
        }

        internal async Task HandleGetColumnInfoRequest(
            GetColumnInfoParams parameters,
            RequestContext<GetColumnInfoResponse> requestContext)
        {
            Logger.Verbose(nameof(HandleGetColumnInfoRequest));

            try
            {
                Prose.BcpProcess process = await ExecuteSessionOperation(
                    parameters.OperationId,
                    session => Task.FromResult(session.Process));
                await requestContext.SendResult(new GetColumnInfoResponse
                {
                    ColumnInfo = process.ColumnInfos.Select(CreateColumnInfo).ToArray()
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        private static string ResolveImportConnectionString(
            InsertDataParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.OwnerUri))
            {
                throw new ArgumentException("OwnerUri is required for flat file import.", nameof(parameters));
            }

            if (!ConnectionService.Instance.TryFindConnection(parameters.OwnerUri, out ConnectionInfo connectionInfo) ||
                connectionInfo == null)
            {
                throw new InvalidOperationException(
                    SR.ConnectionServiceListDbErrorNotConnected(parameters.OwnerUri));
            }

            ConnectionDetails details = connectionInfo.ConnectionDetails.Clone();
            if (!string.IsNullOrEmpty(parameters.DatabaseName))
            {
                details.DatabaseName = parameters.DatabaseName;
            }

            if (details.AuthenticationType == "AzureMFA" &&
                !ConnectionService.Instance.EnableSqlAuthenticationProvider)
            {
                throw new InvalidOperationException(
                    "Flat file import for Azure MFA connections requires EnableSqlAuthenticationProvider.");
            }

            return ConnectionService.BuildConnectionString(details);
        }

        internal async Task HandleChangeColumnSettingsRequest(
            ChangeColumnSettingsParams parameters,
            RequestContext<ChangeColumnSettingsResponse> requestContext)
        {
            Logger.Verbose(nameof(HandleChangeColumnSettingsRequest));

            var result = new Result
            {
                Success = true,
                ErrorMessage = null
            };

            try
            {
                await ExecuteSessionOperation(
                    parameters.OperationId,
                    session =>
                    {
                        var columnInfo = session.Process.ColumnInfos[parameters.Index];

                        if (parameters.NewDataType != null)
                        {
                            columnInfo.SqlType = parameters.NewDataType;
                        }

                        if (parameters.NewInPrimaryKey.HasValue)
                        {
                            columnInfo.InPrimaryKey = parameters.NewInPrimaryKey.Value;
                        }

                        if (parameters.NewName != null)
                        {
                            columnInfo.Name = parameters.NewName;
                        }

                        if (parameters.NewNullable.HasValue)
                        {
                            columnInfo.Nullable = parameters.NewNullable.Value;
                        }

                        return Task.CompletedTask;
                    });
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.ToString();
            }

            await requestContext.SendResult(new ChangeColumnSettingsResponse
            {
                Result = result
            });
        }

        internal async Task HandleLearnTransformationRequest(
            LearnTransformationParams parameters,
            RequestContext<LearnTransformationResponse> requestContext)
        {
            try
            {
                var preview = await ExecuteSessionOperation(
                    parameters.OperationId,
                    session => Task.FromResult(session.Process.LearnTransformation(
                        parameters.ColumnNames,
                        parameters.TransformationExamples,
                        parameters.TransformationExampleRowIndices)));

                await requestContext.SendResult(new LearnTransformationResponse
                {
                    TransformationPreview = preview
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        internal async Task HandleSaveTransformationRequest(
            SaveTransformationParams parameters,
            RequestContext<SaveTransformationResponse> requestContext)
        {
            try
            {
                int numTransformations = await ExecuteSessionOperation(
                    parameters.OperationId,
                    session => Task.FromResult(session.Process.SaveTransformationAs(parameters.DerivedColumnName)));

                await requestContext.SendResult(new SaveTransformationResponse
                {
                    NumTransformations = numTransformations
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        internal async Task HandleDisposeSessionRequest(
            DisposeSessionParams parameters,
            RequestContext<DisposeSessionResponse> requestContext)
        {
            try
            {
                ValidateOperationId(parameters.OperationId);
                RemoveSession(parameters.OperationId);
                await requestContext.SendResult(new DisposeSessionResponse
                {
                    Result = new Result
                    {
                        Success = true,
                        ErrorMessage = null
                    }
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendResult(new DisposeSessionResponse
                {
                    Result = new Result
                    {
                        Success = false,
                        ErrorMessage = ex.ToString()
                    }
                });
            }
        }

        private static Prose.BcpProcessType GetProcessType(string fileType)
        {
            switch (fileType)
            {
                case "JSON":
                    return Prose.BcpProcessType.JSON;
                case "TSV":
                case "CSV":
                    return Prose.BcpProcessType.CSV;
                default:
                    return Prose.BcpProcessType.TXT;
            }
        }

        private static void EnsureSuccess(Prose.Result result)
        {
            if (!result.Success)
            {
                throw result.CaughtException ?? new InvalidOperationException(result.ErrorMessage);
            }
        }

        private static string GetErrorMessage(Prose.Result result)
        {
            return result.CaughtException?.ToString() ?? result.ErrorMessage;
        }

        private static ProseDiscoveryResponse CreateDiscoveryResponse(Prose.BcpProcess process)
        {
            return new ProseDiscoveryResponse
            {
                DataPreview = process.DataPreview,
                ColumnInfo = process.ColumnInfos?.Select(CreateColumnInfo).ToArray(),
                ColumnDelimiter = process.ColumnDelimiter,
                FirstRow = process.NumSkippedLines + 1,
                QuoteCharacter = process.QuoteCharacter
            };
        }

        private static ColumnInfo CreateColumnInfo(Prose.ColumnInfo info)
        {
            return new ColumnInfo
            {
                Name = info.Name,
                SqlType = info.SqlType,
                IsNullable = info.Nullable
            };
        }

        private async Task ExecuteSessionOperation(string operationId, Func<FlatFileSession, Task> action)
        {
            await ExecuteSessionOperation<object>(
                operationId,
                async session =>
                {
                    await action(session);
                    return null;
                });
        }

        private async Task<T> ExecuteSessionOperation<T>(string operationId, Func<FlatFileSession, Task<T>> action)
        {
            ValidateOperationId(operationId);

            if (!sessions.TryGetValue(operationId, out FlatFileSession session) || session == null)
            {
                throw new InvalidOperationException(
                    $"No active flat file import session for operation '{operationId}'. Run prose discovery first.");
            }

            await session.Gate.WaitAsync();
            try
            {
                return await action(session);
            }
            finally
            {
                session.Gate.Release();
            }
        }

        private void ReplaceSession(string operationId, FlatFileSession session)
        {
            FlatFileSession previousSession = sessions.AddOrUpdate(
                operationId,
                session,
                (_, __) => session);

            if (!ReferenceEquals(previousSession, session))
            {
                previousSession.Dispose();
            }
        }

        private void RemoveSession(string operationId)
        {
            if (string.IsNullOrEmpty(operationId))
            {
                return;
            }

            if (sessions.TryRemove(operationId, out FlatFileSession session))
            {
                session.Dispose();
            }
        }

        private static void ValidateOperationId(string operationId)
        {
            if (string.IsNullOrEmpty(operationId))
            {
                throw new ArgumentException("OperationId is required for flat file requests.", nameof(operationId));
            }
        }

        private sealed class FlatFileSession : IDisposable
        {
            public FlatFileSession(Prose.BcpProcess process)
            {
                this.Process = process ?? throw new ArgumentNullException(nameof(process));
                this.Gate = new SemaphoreSlim(1, 1);
            }

            public SemaphoreSlim Gate { get; }

            public Prose.BcpProcess Process { get; }

            public void Dispose()
            {
                (this.Process as IDisposable)?.Dispose();
                this.Gate.Dispose();
            }
        }
    }
}
