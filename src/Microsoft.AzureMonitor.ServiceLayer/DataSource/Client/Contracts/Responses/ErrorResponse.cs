using Microsoft.AzureMonitor.ServiceLayer.DataSource.Client.Contracts.Responses.Models;

namespace Microsoft.AzureMonitor.ServiceLayer.DataSource.Client.Contracts.Responses
{
    public class ErrorResponse
    {
        public string Code { get; set; }
        public string CorrelationId { get; set; }
        public InnerErrorModel InnerError { get; set; }
        public string Message { get; set; }
    }
}