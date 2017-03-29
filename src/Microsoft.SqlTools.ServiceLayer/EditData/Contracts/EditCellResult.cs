namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters to return when a cell is updated in isolation
    /// </summary>
    public class EditCellResult
    {
        /// <summary>
        /// The cell after the revert was applied
        /// </summary>
        public EditCell Cell { get; set; }

        /// <summary>
        /// Whether or not the row is dirty after the revert has been applied
        /// </summary>
        public bool IsRowDirty { get; set; }
    }
}