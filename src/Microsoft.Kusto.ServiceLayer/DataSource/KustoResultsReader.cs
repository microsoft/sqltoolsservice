using System.Data;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    internal class KustoResultsReader : DataReaderWrapper
    {
        public KustoResultsReader(IDataReader reader) : base(reader)
        {
        }
    }
}