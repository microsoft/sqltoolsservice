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

        public void InitializeService(IRpcServiceHost serviceHost)
        {
            Logger.Verbose("FlatFile service initialized");

            serviceHost.RegisterRequestHandler(ProseDiscoveryRequest.Type, HandleProseDiscoveryRequest);
            serviceHost.RegisterRequestHandler(InsertDataRequest.Type, HandleInsertDataRequest);
            serviceHost.RegisterRequestHandler(GetColumnInfoRequest.Type, HandleGetColumnInfoRequest);
            serviceHost.RegisterRequestHandler(ChangeColumnSettingsRequest.Type, HandleChangeColumnSettingsRequest);
            serviceHost.RegisterRequestHandler(LearnTransformationRequest.Type, HandleLearnTransformationRequest);
            serviceHost.RegisterRequestHandler(SaveTransformationRequest.Type, HandleSaveTransformationRequest);
            serviceHost.RegisterRequestHandler(DisposeSessionRequest.Type, HandleDisposeSessionRequest);
        }

        internal async Task<ProseDiscoveryResponse> HandleProseDiscoveryRequest(
            ProseDiscoveryParams parameters)
        {
            Logger.Verbose(nameof(HandleProseDiscoveryRequest));

            try
            {
                Validate.IsNotNull(nameof(parameters), parameters);
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

                return response;
            }
            catch (Exception ex)
            {
                throw RpcErrorException.Create(ex.ToString());
            }
        }

        internal async Task<InsertDataResponse> HandleInsertDataRequest(
            InsertDataParams parameters)
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

            return new InsertDataResponse
            {
                Result = new Result
                {
                    Success = success,
                    ErrorMessage = errorMessage
                }
            };
        }

        internal async Task<GetColumnInfoResponse> HandleGetColumnInfoRequest(
            GetColumnInfoParams parameters)
        {
            Logger.Verbose(nameof(HandleGetColumnInfoRequest));

            try
            {
                Prose.BcpProcess process = await ExecuteSessionOperation(
                    parameters.OperationId,
                    session => Task.FromResult(session.Process));
                return new GetColumnInfoResponse
                {
                    ColumnInfo = process.ColumnInfos.Select(CreateColumnInfo).ToArray()
                };
            }
            catch (Exception ex)
            {
                throw RpcErrorException.Create(ex.ToString());
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
            
            return ConnectionService.BuildConnectionString(details);
        }

        internal async Task<ChangeColumnSettingsResponse> HandleChangeColumnSettingsRequest(
            ChangeColumnSettingsParams parameters)
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

            return new ChangeColumnSettingsResponse
            {
                Result = result
            };
        }

        internal async Task<LearnTransformationResponse> HandleLearnTransformationRequest(
            LearnTransformationParams parameters)
        {
            try
            {
                var preview = await ExecuteSessionOperation(
                    parameters.OperationId,
                    session => Task.FromResult(session.Process.LearnTransformation(
                        parameters.ColumnNames,
                        parameters.TransformationExamples,
                        parameters.TransformationExampleRowIndices)));

                return new LearnTransformationResponse
                {
                    TransformationPreview = preview
                };
            }
            catch (Exception ex)
            {
                throw RpcErrorException.Create(ex.ToString());
            }
        }

        internal async Task<SaveTransformationResponse> HandleSaveTransformationRequest(
            SaveTransformationParams parameters)
        {
            try
            {
                int numTransformations = await ExecuteSessionOperation(
                    parameters.OperationId,
                    session => Task.FromResult(session.Process.SaveTransformationAs(parameters.DerivedColumnName)));

                return new SaveTransformationResponse
                {
                    NumTransformations = numTransformations
                };
            }
            catch (Exception ex)
            {
                throw RpcErrorException.Create(ex.ToString());
            }
        }

        internal async Task<DisposeSessionResponse> HandleDisposeSessionRequest(
            DisposeSessionParams parameters)
        {
            try
            {
                ValidateOperationId(parameters.OperationId);
                RemoveSession(parameters.OperationId);
                return new DisposeSessionResponse
                {
                    Result = new Result
                    {
                        Success = true,
                        ErrorMessage = null
                    }
                };
            }
            catch (Exception ex)
            {
                return new DisposeSessionResponse
                {
                    Result = new Result
                    {
                        Success = false,
                        ErrorMessage = ex.ToString()
                    }
                };
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
