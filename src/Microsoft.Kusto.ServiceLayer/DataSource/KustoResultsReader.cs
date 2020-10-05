using System.Collections.Generic;
using System.Data;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    internal class KustoResultsReader : DataReaderWrapper
    {
        private DataSet _resultDataSet;

        /// <summary>
        /// Kusto returns atleast 4 results tables - QueryResults(sometimes more than one), QueryProperties, QueryStatus and Query Results Metadata Table. 
        /// ADS just needs query results. When returning query results we need to trim off the last 3 tables. 
        /// </summary> 
        public KustoResultsReader(IDataReader[] readers)
            : base()
        {
            // Read out all tables
            List<DataTable> results = new List<DataTable>();

            foreach (var reader in readers)
            {
                while (!(reader?.IsClosed ?? true))
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader); // This calls NextResult on the reader
                    results.Add(dt);
                }
                
                // Trim results
                if(results.Count > 3) results.RemoveRange(results.Count - 3, 3);
            }

            // Create a DataReader for the trimmed set
            _resultDataSet = new DataSet();
            for(int i = 0; i < results.Count; i++)
            {
                results[i].TableName = "Table_" + i;
                _resultDataSet.Tables.Add(results[i]);
            }

            SetDataReader(_resultDataSet.CreateDataReader());
        }
    }
}