using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;

namespace Microsoft.AzureMonitor.ServiceLayer.DataSource
{
    public class MonitorQueryResult
    {
        public DbColumnWrapper[] Columns { get; set; }
        
        public ResultSetSubset ResultSubset { get; set; }
    }
}