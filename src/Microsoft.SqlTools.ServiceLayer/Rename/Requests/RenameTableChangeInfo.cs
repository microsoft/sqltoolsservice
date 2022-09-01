namespace Microsoft.SqlTools.ServiceLayer.Rename.Requests
{
    public enum ChangeType
    {
        Table, Column
    }
    public class RenameTableChangeInfo
    {
        public ChangeType Type { get; set; }
        public string NewName { get; set; }
    }
}