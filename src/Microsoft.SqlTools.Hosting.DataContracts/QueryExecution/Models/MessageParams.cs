namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters to be sent back with a message notification
    /// </summary>
    public class MessageParams
    {
        /// <summary>
        /// URI for the editor that owns the query
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The message that is being returned
        /// </summary>
        public ResultMessage Message { get; set; }
    }
}