namespace Microsoft.SqlTools.Hosting.DataContracts.Scripting.Models
{
    /// <summary>
    /// Scripting Operation type
    /// </summary>
    public enum ScriptingOperationType
    {
        Select = 0,
        Create = 1,
        Insert = 2,
        Update = 3,
        Delete = 4,
        Execute = 5,
        Alter = 6
    }
}