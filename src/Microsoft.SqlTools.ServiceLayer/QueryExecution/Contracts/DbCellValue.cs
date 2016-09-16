

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    public class DbCellValue
    {
        public string DisplayValue { get; set; }
        internal object RawObject { get; set; }
        public bool IsNull { get; set; }
        public bool IsXml { get; set; }
        public bool IsBinary { get; set; }
        public bool IsText { get; set; }
    }
}
