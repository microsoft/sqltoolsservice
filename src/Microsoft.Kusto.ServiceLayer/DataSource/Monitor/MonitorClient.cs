using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Azure.OperationalInsights.Models;
using Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses;
using Microsoft.Rest;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor
{
    public class MonitorClient
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
            return await _queryClient.QueryAsync(query, cancellationToken: cancellationToken);
        }

        public QueryResults Query(string query)
        {
            return _queryClient.Query(query);
        }

        ~MonitorClient()
        {
            _httpClient.Dispose();
            _queryClient.Dispose();
        }
    }
}