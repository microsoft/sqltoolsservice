using System.Data.Common;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    public class ResultSetSummary
    {
        /// <summary>
        /// The ID of the result set within the query results
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The number of rows that was returned with the resultset
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// Details about the columns that are provided as solutions
        /// </summary>
        public DbColumn[] ColumnInfo { get; set; }
    }
}
