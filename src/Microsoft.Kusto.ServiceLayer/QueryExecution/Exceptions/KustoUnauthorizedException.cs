using System;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Exceptions
{
    public class KustoUnauthorizedException : Exception
    {
        public KustoUnauthorizedException(string message) : base (message)
        {
        }
    }
}