
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.SqlCopilot.Cartridges;
using Microsoft.SqlServer.SqlCopilot.Common;
using OpenAI.Chat;

/// <summary>
/// Manages tool calls and their responses across conversations, providing caching and recursion protection
/// </summary>
public class ToolCallManager
{
    private readonly ConcurrentDictionary<string, ToolCallContext> _activeContexts = new();
    private readonly SqlExecAndParse _sqlExecAndParseHelper;
    private const int MaxCallsPerTool = 3;

    public ToolCallManager(SqlExecAndParse sqlExecAndParseHelper)
    {
        _sqlExecAndParseHelper = sqlExecAndParseHelper;
    }

    public async Task<string> HandleToolCallAsync(
        string conversationUri,
        ChatTool tool,
        string parameters,
        CancellationToken cancellationToken = default)
    {
        var context = GetOrCreateContext(conversationUri);
        var callKey = CreateCallKey(tool.FunctionName, parameters);

        // Check cache first
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
            Name = tool.FunctionName,
            Parameters = parameters
        };

        // Prevent infinite recursion
        if (IsRecursiveCall(context, tool.FunctionName))
        {
            var message = $"Too many consecutive calls to {tool.FunctionName}";
            SqlCopilotTrace.WriteErrorEvent(SqlCopilotTraceEvents.KernelFunctionCall, message);
            throw new InvalidOperationException(message);
        }

        // Execute tool
        try
        {
            context.CurrentCall = currentCall;
            var result = await _sqlExecAndParseHelper.ExecuteToolAsync(
                tool.FunctionName, 
                parameters, 
                cancellationToken);
            
            currentCall.ToolResponse = result;
            CacheToolCall(context, currentCall);
            
            return result;
        }
        catch (Exception ex)
        {
            SqlCopilotTrace.WriteErrorEvent(
                SqlCopilotTraceEvents.KernelFunctionCall
,
                $"Tool execution failed: {ex.Message}");
            throw;
        }
        finally
        {
            context.CurrentCall = null;
        }
    }

    // And for the execution, we need to use what's in SqlExecAndParse
    // Instead of our imaginary ExecuteToolAsync
    private async Task<string> ExecuteTool(ChatTool tool, string parameters)
    {
        // We're actually working with the existing tool calls through the copilot service
        try 
        {
            var result = await _sqlExecAndParseHelper.ExecuteSqlQueryAsync(
                parameters,  // This is usually a SQL query
                false       // not a stored proc
            );
            return result;
        }
        catch (Exception ex)
        {
            SqlCopilotTrace.WriteErrorEvent(
                SqlCopilotTraceEvents.KernelFunctionCall,
                $"Failed to execute {tool.FunctionName}: {ex.Message}");
            throw;
        }
    }

    private ToolCallContext GetOrCreateContext(string conversationUri) =>
        _activeContexts.GetOrAdd(conversationUri, _ => new ToolCallContext());

    private static string CreateCallKey(string toolName, string parameters) =>
        $"{toolName}:{parameters}";

    private static bool TryGetCachedResult(
        ToolCallContext context,
        string toolName,
        string parameters,
        out string? result)
    {
        result = default;
        
        if (!context.CallHistory.TryGetValue(toolName, out var calls))
            return false;

        var previousCall = calls.FirstOrDefault(c => 
            c.Parameters == parameters && c.ToolResponse != null);
            
        if (previousCall == null)
            return false;

        result = previousCall.ToolResponse;
        return true;
    }

    private static bool IsRecursiveCall(ToolCallContext context, string toolName)
    {
        if (!context.CallHistory.TryGetValue(toolName, out var calls))
            return false;

        return calls.Count >= MaxCallsPerTool;
    }

    private static void CacheToolCall(ToolCallContext context, ToolCall call)
    {
        if (!context.CallHistory.TryGetValue(call.Name, out var calls))
        {
            calls = new List<ToolCall>();
            context.CallHistory[call.Name] = calls;
        }

        calls.Add(call);
    }
}

// This is what we actually have to work with
public interface IToolCall 
{
    string FunctionName { get; }  // Not Name - it's FunctionName in the actual code
    string FunctionDescription { get; }
    string FunctionParameters { get; }  // Not just Parameters
}

public class ToolCallContext
{
    public IToolCall? CurrentCall { get; set; }
    public Dictionary<string, List<ToolCall>> CallHistory { get; } = new();
}

public class ToolCall : IToolCall
{
    public string Name { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
    public string? ToolResponse { get; set; }
}