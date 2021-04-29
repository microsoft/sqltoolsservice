namespace Microsoft.SqlTools.Hosting.DataContracts.Scripting.Models
{
    /// <summary>
    /// Parameters returned from a script request.
    /// </summary>
    public class ScriptingResult
    {
        public string OperationId { get; set; }

        public string Script { get; set; }
    }
}