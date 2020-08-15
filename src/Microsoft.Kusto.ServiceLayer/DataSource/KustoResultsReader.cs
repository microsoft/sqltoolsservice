using Microsoft.Kusto.ServiceLayer.QueryExecution;
using System.Collections.Generic;
using System.Data;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    internal class KustoResultsReader : DataReaderWrapper
    {
        public KustoResultsReader(IDataReader reader)
            : base(reader)
        {
        }

        /// <summary>
        /// Kusto returns atleast 4 results tables - QueryResults(sometimes more than one), QueryProperties, QueryStatus and Query Results Metadata Table. 
        /// When returning query results we need to trim off the last 3 tables as we want the caller to only read results table. 
        /// </summary>
        public void SanitizeResults(List<ResultSet> resultSets)
        {
            if (resultSets.Count > 3)
            {
                resultSets.RemoveRange(resultSets.Count - 3, 3);
            }
        }
    }
}