using System;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Exceptions
{
    public class DataSourceUnauthorizedException : Exception
    {
        public DataSourceUnauthorizedException(Exception ex) : base (ex.Message, ex)
        {
        }
    }
}