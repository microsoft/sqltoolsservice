namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public partial class KustoDataSource
    {
        public class ColumnInfo
        {
            /// <summary>
            /// The table name.
            /// </summary>
            public string Table { get; set; }

            /// <summary>
            /// The column name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The data type.
            /// </summary>
            public string DataType { get; set; }
        }
    }
}