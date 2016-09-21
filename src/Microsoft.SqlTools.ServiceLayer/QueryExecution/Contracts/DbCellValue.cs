

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    public class DbCellValue
    {
        public string DisplayValue { get; set; }
        internal object RawObject { get; set; }
    }
}
