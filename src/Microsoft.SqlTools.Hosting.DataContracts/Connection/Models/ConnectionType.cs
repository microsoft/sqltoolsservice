namespace Microsoft.SqlTools.Hosting.DataContracts.Connection.Models
{
    /// <summary>
    /// String constants that represent connection types. 
    /// 
    /// Default: Connection used by the editor. Opened by the editor upon the initial connection. 
    /// Query: Connection used for executing queries. Opened when the first query is executed.
    /// </summary>
    public static class ConnectionType
    {
        public const string Default = "Default";
        public const string Query = "Query";
        public const string Edit = "Edit";
        public const string ObjectExplorer = "ObjectExplorer";
        public const string Dashboard = "Dashboard";
        public const string GeneralConnection = "GeneralConnection";
    }
}