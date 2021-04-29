namespace Microsoft.SqlTools.Hosting.DataContracts.Scripting.Models
{
    /// <summary>
    /// Parameters sent to when a scripting operation has completed.
    /// </summary>
    public class ScriptingCompleteParams: ScriptingEventParams
    {
        /// <summary>
        /// Get or sets the error details for an error that occurred during the scripting operation.
        /// </summary>
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Get or sets the error message for an error that occurred during the scripting operation.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Get or sets a value to indicate an error occurred during the scripting operation.
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Get or sets a value to indicate the scripting operation was canceled.
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// Get or sets a value to indicate the scripting operation successfully completed.
        /// </summary>
        public bool Success { get; set; }
    }
}