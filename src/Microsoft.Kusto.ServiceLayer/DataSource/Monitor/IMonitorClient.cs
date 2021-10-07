using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.OperationalInsights.Models;
using Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor
{
    public interface IMonitorClient
    {
        string WorkspaceId { get; }
        WorkspaceResponse LoadMetadata(bool refresh = false);
        Task<QueryResults> QueryAsync(string query, CancellationToken cancellationToken);
        QueryResults Query(string query);
    }
}