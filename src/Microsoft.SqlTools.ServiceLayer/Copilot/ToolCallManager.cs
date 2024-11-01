//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#if false

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.SqlCopilot.Common;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Copilot.Contracts;

/// <summary>
/// Represents a tool call with its parameters and response
/// </summary>
public interface IToolCall
{
    string FunctionName { get; }
    string FunctionDescription { get; }
    string FunctionParameters { get; }
    string? ToolResponse { get; set; }
}

/// <summary>
/// Implementation of IToolCall that tracks function execution details
/// </summary>
public class ToolCall : IToolCall
{
    public string FunctionName { get; set; } = string.Empty;
    public string FunctionDescription { get; set; } = string.Empty;
    public string FunctionParameters { get; set; } = string.Empty;
    public string? ToolResponse { get; set; }
}

/// <summary>
/// Manages the context and state for a series of tool calls within a conversation
/// </summary>
public class ToolCallContext
{
    public IToolCall? CurrentCall { get; set; }
    public Dictionary<string, List<ToolCall>> CallHistory { get; } = new();
}

/// <summary>
/// Provides execution services for SQL queries with proper connection management
/// </summary>
//public class SqlExecutionService
//{
//    private readonly SqlConnection _sqlConnection;

//    public SqlExecutionService(SqlConnection connection)
//    {
//        _sqlConnection = connection ?? throw new ArgumentNullException(nameof(connection));
//    }

//    public async Task<string> ExecuteToolAsync(string functionName, string parameters, CancellationToken cancellationToken)
//    {
//        // For now, we treat all tool calls as SQL queries
//        return await ExecuteSqlQueryAsync(parameters, false, cancellationToken);
//    }

//    public async Task<string> ExecuteSqlQueryAsync(string query, bool isStoredProc, CancellationToken cancellationToken)
//    {
//        try
//        {
//            using var command = new SqlCommand(query, _sqlConnection);
//            command.CommandType = isStoredProc ? CommandType.StoredProcedure : CommandType.Text;

//            using var reader = await command.ExecuteReaderAsync(cancellationToken);
//            var result = new System.Text.StringBuilder();

//            do
//            {
//                while (await reader.ReadAsync(cancellationToken))
//                {
//                    for (var i = 0; i < reader.FieldCount; i++)
//                    {
//                        result.Append(reader.GetName(i))
//                              .Append(": ")
//                              .Append(reader.GetValue(i))
//                              .AppendLine();
//                    }
//                }
//                result.AppendLine();
//            } while (await reader.NextResultAsync(cancellationToken));

//            return result.ToString();
//        }
//        catch (Exception ex)
//        {
//            SqlCopilotTrace.WriteErrorEvent(
//                SqlCopilotTraceEvents.KernelFunctionCall,
//                $"SQL execution failed: {ex.Message}");
//            return $"Error executing query: {ex.Message}";
//        }
//    }
//}

/// <summary>
/// Manages tool call execution, caching, and recursion prevention
/// </summary>
public class ToolCallManager
{
    private readonly ConcurrentDictionary<string, ToolCallContext> _activeContexts = new();
    private readonly SqlExecutionService _sqlExecutionService;
    private const int MaxCallsPerTool = 3;  // Prevent infinite recursion

    public ToolCallManager(SqlExecutionService sqlExecutionService)
    {
        _sqlExecutionService = sqlExecutionService ?? throw new ArgumentNullException(nameof(sqlExecutionService));
    }

    /// <summary>
    /// Handles execution of a tool call, including caching and recursion checks
    /// </summary>
    public async Task<string> HandleToolCallAsync(
        string conversationUri,
        LanguageModelChatTool  tool,
        string parameters,
        CancellationToken cancellationToken = default)
    {
        var context = GetOrCreateContext(conversationUri);
        var callKey = CreateCallKey(tool.FunctionName, parameters);

        // Check cache first to avoid duplicate work
        if (TryGetCachedResult(context, tool.FunctionName, parameters, out string? cachedResult))
        {
            SqlCopilotTrace.WriteInfoEvent(
                SqlCopilotTraceEvents.KernelFunctionCall,
                $"Cache hit for tool {tool.FunctionName}");
            return cachedResult;
        }

        // Track new call
        var currentCall = new ToolCall
        {
            FunctionName = tool.FunctionName,
            FunctionParameters = parameters
        };

        // Prevent infinite recursion
        if (IsRecursiveCall(context, tool.FunctionName))
        {
            var message = $"Too many consecutive calls to {tool.FunctionName}";
            SqlCopilotTrace.WriteErrorEvent(
                SqlCopilotTraceEvents.KernelFunctionCall,
                message);
            throw new InvalidOperationException(message);
        }

        // Execute tool
        try
        {
            context.CurrentCall = currentCall;
            var result = await _sqlExecutionService.ExecuteToolAsync(
                tool.FunctionName,
                parameters,
                cancellationToken);

            currentCall.ToolResponse = result;
            CacheToolCall(context, currentCall);

            return result;
        }
        finally
        {
            context.CurrentCall = null;
        }
    }

    private ToolCallContext GetOrCreateContext(string conversationUri) =>
        _activeContexts.GetOrAdd(conversationUri, _ => new ToolCallContext());

    private static string CreateCallKey(string functionName, string parameters) =>
        $"{functionName}:{parameters}";

    private static bool TryGetCachedResult(
        ToolCallContext context,
        string functionName,
        string parameters,
        out string? result)
    {
        result = null;

        if (!context.CallHistory.TryGetValue(functionName, out var calls))
            return false;

        var previousCall = calls.FirstOrDefault(c =>
            c.FunctionParameters == parameters && c.ToolResponse != null);

        if (previousCall == null)
            return false;

        result = previousCall.ToolResponse;
        return true;
    }

    private static bool IsRecursiveCall(ToolCallContext context, string functionName)
    {
        if (!context.CallHistory.TryGetValue(functionName, out var calls))
            return false;

        return calls.Count >= MaxCallsPerTool;
    }

    private static void CacheToolCall(ToolCallContext context, ToolCall call)
    {
        if (!context.CallHistory.TryGetValue(call.FunctionName, out var calls))
        {
            calls = new List<ToolCall>();
            context.CallHistory[call.FunctionName] = calls;
        }

        calls.Add(call);
    }
}

#endif
