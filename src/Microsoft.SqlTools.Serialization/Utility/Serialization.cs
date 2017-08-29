using Microsoft.SqlTools.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Serialization.Utility
{
    public class SaveResultsInfo
    {
        public string SaveFormat { get; set; }
        public string SavePath { get; set; }
        public DbCellValue[][] Rows { get; set; }
        public bool IsLast { get; set; }

        public SaveResultsInfo(string saveFormat, 
            string savePath, 
            DbCellValue[][] rows, 
            bool isLast)
        {
            this.SaveFormat = saveFormat;
            this.SavePath = savePath;
            this.Rows = rows;
            this.IsLast = isLast;
        }
    }

    public class SaveAsRequest
    {
        public static readonly
            RequestType<SaveResultsInfo, SaveResultRequestResult> Type =
            RequestType<SaveResultsInfo, SaveResultRequestResult>.Create("query/saveAs");
    }
}
