using System;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Connection;

public interface ISqlExecutionService 
{
    Task<string> ExecuteSqlQueryAsync(string query, bool isStoredProc, string[]? parameters = null);
    SqlConnection? GetCurrentConnection();
}

public class SqlExecutionService : ISqlExecutionService 
{
    private readonly SqlConnection _sqlConnection;

    public SqlExecutionService(SqlConnection connection) 
    {
        _sqlConnection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public SqlConnection? GetCurrentConnection() => _sqlConnection;

    public async Task<string> ExecuteSqlQueryAsync(string query, bool isStoredProc, string[]? parameters = null)
    {
        if (_sqlConnection == null || _sqlConnection.State != ConnectionState.Open)
        {
            return "Not connected to a database";
        }

        try
        {
            // Create command
            using var command = new SqlCommand(query, _sqlConnection);
            command.CommandType = isStoredProc ? CommandType.StoredProcedure : CommandType.Text;

            // Add parameters if any
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var paramParts = param.Split('=');
                    if (paramParts.Length == 2)
                    {
                        command.Parameters.AddWithValue(paramParts[0], paramParts[1]);
                    }
                }
            }

            // Execute and format results
            using var reader = await command.ExecuteReaderAsync();
            var result = new StringBuilder();
            
            do
            {
                while (await reader.ReadAsync())
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        result.Append(reader.GetName(i))
                              .Append(": ")
                              .Append(reader.GetValue(i))
                              .AppendLine();
                    }
                }
                result.AppendLine();
            } while (await reader.NextResultAsync());

            return result.ToString();
        }
        catch (Exception ex)
        {
            SqlCopilotTrace.WriteErrorEvent(
                SqlCopilotTraceEvents.KernelFunctionCall,
                $"SQL execution failed: {ex.Message}");
                
            return $"The following error occurred querying the database: {ex.Message}";
        }
    }
}

// And here's a factory to help create it
public static class SqlExecutionServiceFactory 
{
    public static async Task<ISqlExecutionService> CreateAsync(string connectionUri)
    {
        DbConnection dbConnection = await ConnectionService.Instance.GetOrOpenConnection(
            connectionUri, 
            ConnectionType.Default);
            
        if (!ConnectionService.Instance.TryGetAsSqlConnection(dbConnection, out var sqlConnection))
        {
            throw new InvalidOperationException("Could not get SQL connection");
        }

        return new SqlExecutionService(sqlConnection);
    }
}