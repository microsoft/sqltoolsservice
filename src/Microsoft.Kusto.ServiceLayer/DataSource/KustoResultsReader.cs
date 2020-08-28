using System.Data;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    internal class KustoResultsReader : DataReaderWrapper
    {
        public KustoResultsReader(IDataReader reader) : base(reader)
        {
        }
        
        /// <summary>
        /// Kusto returns 3 results tables - QueryResults, QueryProperties, QueryStatus. When returning query results	
        /// we want the caller to only read the first table. We override the NextResult function here to only return one table	
        /// from the IDataReader.	
        /// </summary>	
        public override bool NextResult()	
        {	
            return false;	
        }
    }
}