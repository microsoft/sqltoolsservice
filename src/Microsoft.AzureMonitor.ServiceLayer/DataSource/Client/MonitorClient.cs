using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Azure.OperationalInsights.Models;
using Microsoft.AzureMonitor.ServiceLayer.DataSource.Client.Contracts.Responses;
using Microsoft.Rest;

namespace Microsoft.AzureMonitor.ServiceLayer.DataSource.Client
{
    public class MonitorClient
    {
        private readonly OperationalInsightsDataClient _queryClient;
        private readonly string _workspaceId;
        private readonly string _token;
        private const string BaseUri = "https://api.loganalytics.io/v1/workspaces";
        
        public string WorkspaceId => _workspaceId;

        public MonitorClient(string workspaceId, string token)
        {
            _workspaceId = workspaceId;
            _token = token;
            _queryClient = new OperationalInsightsDataClient(new TokenCredentials(token))
            {
                WorkspaceId = workspaceId
            };
        }

        public WorkspaceResponse LoadMetadata()
        {
            using (var httpClient = GetHttpClient())
            {
                var url = $"{BaseUri}/{_workspaceId}/metadata";
                var httpResponseMessage = httpClient.GetAsync(url).Result;
                var results = httpResponseMessage.Content.ReadAsStringAsync().Result;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<WorkspaceResponse>(results, options);
            }
        }

        private HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
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
    }
}