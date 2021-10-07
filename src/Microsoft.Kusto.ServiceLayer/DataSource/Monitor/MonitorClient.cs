using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Language;
using Kusto.Language.Editor;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Azure.OperationalInsights.Models;
using Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses;
using Microsoft.Rest;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor
{
    public class MonitorClient : IMonitorClient
    {
        private readonly OperationalInsightsDataClient _queryClient;
        private readonly HttpClient _httpClient;
        private readonly string _workspaceId;
        private readonly string _token;
        private const string BaseUri = "https://api.loganalytics.io/v1/workspaces";
        private WorkspaceResponse _metadata;

        public string WorkspaceId => _workspaceId;

        public MonitorClient(string workspaceId, string token)
        {
            _workspaceId = workspaceId;
            _token = token;
            _httpClient = GetHttpClient(token);
            _queryClient = new OperationalInsightsDataClient(new TokenCredentials(token))
            {
                WorkspaceId = workspaceId
            };
        }

        public WorkspaceResponse LoadMetadata(bool refresh = false)
        {
            if (_metadata != null && !refresh)
            {
                return _metadata;
            }

            var url = $"{BaseUri}/{_workspaceId}/metadata";
            var httpResponseMessage = _httpClient.GetAsync(url).Result;
            var results = httpResponseMessage.Content.ReadAsStringAsync().Result;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _metadata = JsonSerializer.Deserialize<WorkspaceResponse>(results, options);

            if (_metadata?.Tables is null && _metadata?.Workspaces is null && _metadata?.TableGroups is null)
            {
                var errorMessage = JsonSerializer.Deserialize<ErrorResponse>(results, options);
                var builder = new StringBuilder();
                builder.AppendLine(
                    "The Log Analytics Workspace can not be reached. Please validate the Workspace ID, the correct tenant is selected, and that you have access to the workspace. ");
                builder.AppendLine($"Error Message: {errorMessage?.Error?.Message}");
                throw new Exception(builder.ToString());
            }
            
            return _metadata;
        }

        private HttpClient GetHttpClient(string token)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return httpClient;
        }

        public async Task<QueryResults> QueryAsync(string query, CancellationToken cancellationToken)
        {
            try
            {
                var minimizedQuery = MinimizeQuery(query);
                return await _queryClient.QueryAsync(minimizedQuery, cancellationToken: cancellationToken);
            }
            catch (ErrorResponseException ex)
            {
                var message = $"{ex.Body.Error.Innererror.Message} {ex.Body.Error.Innererror?.Innererror?.Message}";
                throw new Exception(message, ex);
            }
        }

        public QueryResults Query(string query)
        {
            try
            {
                var minimizedQuery = MinimizeQuery(query);
                return _queryClient.Query(minimizedQuery);
            }
            catch (ErrorResponseException ex)
            {
                var message = $"{ex.Body.Error.Innererror.Message} {ex.Body.Error.Innererror?.Innererror?.Message}";
                throw new Exception(message, ex);
            }
        }

        private string MinimizeQuery(string query)
        {
            var script = CodeScript.From(query, GlobalState.Default);
            return script.Blocks[0].Service.GetMinimalText(MinimalTextKind.RemoveLeadingWhitespaceAndComments);
        }

        ~MonitorClient()
        {
            _httpClient.Dispose();
            _queryClient.Dispose();
        }
    }
}