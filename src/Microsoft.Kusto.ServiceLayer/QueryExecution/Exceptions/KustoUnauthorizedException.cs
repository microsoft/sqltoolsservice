using System;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Exceptions
{
    public class KustoUnauthorizedException : Exception
    {
        public KustoUnauthorizedException(Exception ex) : base (ex.Message, ex)
        {
        }
    }
}