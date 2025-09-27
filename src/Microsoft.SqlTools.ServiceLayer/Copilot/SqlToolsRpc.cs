//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Scriptoria.Common;
using Microsoft.Scriptoria.Interfaces;
using Microsoft.Scriptoria.Models;
using Microsoft.SqlTools.Utility;
using ScriptoriaCommonDefs;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    public class ChatResponseHandler
    {
        private readonly CopilotConversation _conversation;

        public ChatResponseHandler(CopilotConversation conversation)
        {
            _conversation = conversation;
        }

        public event Action<ChatResponseEventArgs> OnChatResponse;
        public event Action<ProcessingUpdateEventArgs> OnProcessingUpdate;

        public async Task HandlePartialResponse(string exchangeId, string response)
        {
            await Task.Run(() => OnChatResponse?.Invoke(new ChatResponseEventArgs
            {
                UpdateType = ResponseUpdateType.PartialResponseUpdate,
                ChatExchangeId = exchangeId,
                PartialResponse = response,
                Conversation = _conversation
            }));
        }

        public async Task NotifyExchangeStarted(string exchangeId)
        {
            await Task.Run(() => OnChatResponse?.Invoke(new ChatResponseEventArgs
            {
                UpdateType = ResponseUpdateType.Started,
                ChatExchangeId = exchangeId,
                Conversation = _conversation
            }));
        }

        public async Task NotifyExchangeComplete(string exchangeId)
        {
            await Task.Run(() => OnChatResponse?.Invoke(new ChatResponseEventArgs
            {
                UpdateType = ResponseUpdateType.Completed,
                ChatExchangeId = exchangeId,
                Conversation = _conversation
            }));
        }

        public async Task NotifyExchangeCanceled(string exchangeId)
        {
            await Task.Run(() => OnChatResponse?.Invoke(new ChatResponseEventArgs
            {
                UpdateType = ResponseUpdateType.Canceled,
                ChatExchangeId = exchangeId,
                Conversation = _conversation
            }));
        }

        public async Task<string> HandleProcessingUpdate(
            string exchangeId,
            ProcessingUpdateType updateType,
            Dictionary<string, string> parameters)
        {
            await Task.Run(() => OnProcessingUpdate?.Invoke(new ProcessingUpdateEventArgs
            {
                ChatExchangeId = exchangeId,
                UpdateType = updateType,
                Parameters = parameters
            }));
            return "success";
        }
    }

    public class SqlToolsRpcClient
    {
        private readonly ICartridgeDataAccess _sqlService;
        private readonly ChatResponseHandler _responseHandler;

        public SqlToolsRpcClient(
            ICartridgeDataAccess sqlService,
            ChatResponseHandler responseHandler)
        {
            _sqlService = sqlService;
            _responseHandler = responseHandler;
        }

        public async Task<T> InvokeAsync<T>(string method, params object[] parameters)
        {
            try
            {
                return method switch
                {
                    "ExecuteSqlQueryAsync" => await HandleSqlQuery<T>(parameters),
                    "HandlePartialResponseAsync" => await HandleChatResponse<T>(parameters),
                    "ChatExchangeStartedAsync" => await HandleExchangeStarted<T>(parameters),
                    "ChatExchangeCompleteAsync" => await HandleExchangeComplete<T>(parameters),
                    "ChatExchangeCanceledAsync" => await HandleExchangeCanceled<T>(parameters),
                    "PromptProcessingUpdate" => await HandleProcessingUpdate<T>(parameters),
                    _ => throw new NotImplementedException($"Method not implemented: {method}")
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC call failed: {ex.Message}");
                throw;
            }
        }

        private async Task<T> HandleSqlQuery<T>(object[] parameters)
        {
            if (parameters.Length < 2)
                throw new ArgumentException("Not enough parameters for SQL query");

            string query = (string)parameters[0];
            bool isStoredProc = (bool)parameters[1];
            object[] sqlParams = parameters.Length > 2 ?
                (object[])parameters[2] : Array.Empty<object>();

            var result = await _sqlService.ExecuteQueryAsync(
                new QueryContentDescriptor { 
                    Query = query, 
                    ExecuteStoredProcedure = isStoredProc,
                    QueryParameters = sqlParams.Select(p => p.ToString()).ToList()
                }, CancellationToken.None);

            return typeof(T) == typeof(string)
                ? (T)(object)result.Result  // Extract string result from QueryResult
                : throw new InvalidCastException("Invalid return type");
        }

        private async Task<T> HandleChatResponse<T>(object[] parameters)
        {
            if (parameters.Length < 2)
                throw new ArgumentException("Not enough parameters for chat response");

            await _responseHandler.HandlePartialResponse(
                (string)parameters[0],
                (string)parameters[1]);

            return default;
        }

        private async Task<T> HandleExchangeStarted<T>(object[] parameters)
        {
            await _responseHandler.NotifyExchangeStarted((string)parameters[0]);
            return default;
        }

        private async Task<T> HandleExchangeComplete<T>(object[] parameters)
        {
            await _responseHandler.NotifyExchangeComplete((string)parameters[0]);
            return default;
        }

        private async Task<T> HandleExchangeCanceled<T>(object[] parameters)
        {
            await _responseHandler.NotifyExchangeCanceled((string)parameters[0]);
            return default;
        }

        private async Task<T> HandleProcessingUpdate<T>(object[] parameters)
        {
            if (parameters.Length < 3)
                throw new ArgumentException("Not enough parameters for processing update");

            var result = await _responseHandler.HandleProcessingUpdate(
                (string)parameters[0],
                (ProcessingUpdateType)parameters[1],
                (Dictionary<string, string>)parameters[2]);

            return typeof(T) == typeof(string)
                ? (T)(object)result
                : throw new InvalidCastException("Invalid return type");
        }
    }

    // <summary>
    // Provides execution services for SQL queries with proper connection management
    // </summary>
    public class SqlExecutionService : ICartridgeDataAccess, ICartridgeListener
    {
        private readonly SqlConnection _sqlConnection;

        public SqlExecutionService(SqlConnection connection)
        {
            _sqlConnection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task<QueryResult> ExecuteQueryAsync(QueryContentDescriptor queryContentDescriptor, CancellationToken cancellationToken)
        {
            // Extract parameters from the new interface
            string query = queryContentDescriptor?.Query ?? throw new ArgumentNullException(nameof(queryContentDescriptor));
            object[] parameters = queryContentDescriptor.QueryParameters?.ToArray() ?? new object[0];
            bool execSP = queryContentDescriptor.ExecuteStoredProcedure;
            
            if (_sqlConnection != null && _sqlConnection.State == ConnectionState.Open)
            {
                // create a command to execute the sql
                var command = new SqlCommand(query, _sqlConnection);

                // add the parameters to the command
                foreach (var param in parameters)
                {
                    var paramStr = param.ToString();
                    var paramParts = paramStr.Split('=');
                    _ = command.Parameters.AddWithValue(paramParts[0], paramParts[1]);
                }

                // if it is a stored procedure, set the command type to stored procedure
                if (execSP)
                {
                    command.CommandType = CommandType.StoredProcedure;
                }

                try
                {
                    // execute the query async and then process the results, returning them as a string
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        var result = new System.Text.StringBuilder();
                        do
                        {
                            while (reader.Read())
                            {
                                for (var i = 0; i < reader.FieldCount; i++)
                                {
                                    _ = result.Append(reader.GetName(i) + ": " + reader.GetValue(i) + "\n");
                                }
                            }
                            _ = result.Append("\n");
                        } while (reader.NextResult());

                        // Wrap string result in QueryResult
                        return new QueryResult(result.ToString(), null, null);
                    }
                }
                catch (Exception e)
                {
                    // return the error to the LLM
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    // Wrap error result in QueryResult
                    return new QueryResult($"The following error occured querying the database: {e.Message}", e, null);
                }
            }
            else
            {
                // Wrap connection error in QueryResult
                return new QueryResult("Not connected to a database", null, null);
            }
        }

        /// <summary>
        /// Forward the processing update to the client
        /// </summary>
        /// <param name="updateType"></param>
        /// <param name="updateParameters"></param>
        /// <returns></returns>
        public Task SendProcessUpdate(ProcessingUpdateType updateType, Dictionary<string, string> updateParameters)
        {
            // not used in the evaluation framework
            return Task.CompletedTask;
        }

        /// <summary>
        /// forward the approval request to the client
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <param name="requestId"></param>
        /// <param name="requestText"></param>
        /// <returns></returns>
        public Task<string> RequestUserApprovalAsync(string exchangeId, Guid requestId, string requestText)
        {
            // not used in the evaluation framework
            return Task.FromResult(string.Empty);
        }

        public async Task<HttpResponseMessage> ExecuteHttpRequestAsync(HttpRequestDescriptor httpRequestDescriptor, CancellationToken cancellationToken)
        {
            // For now, return a not implemented response
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.NotImplemented)
            {
                Content = new System.Net.Http.StringContent("HTTP requests not supported in this context")
            };
            return await Task.FromResult(response);
        }
    }
}
