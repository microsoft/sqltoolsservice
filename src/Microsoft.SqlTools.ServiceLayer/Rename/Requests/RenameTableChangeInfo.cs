using System.ComponentModel;

namespace Microsoft.SqlTools.ServiceLayer.Rename.Requests
{
    public enum ChangeType
    {
        TABLE,
        COLUMN

    }
    public class RenameTableChangeInfo
    {
        public ChangeType Type { get; set; }
        public string NewName { get; set; }
    }
}