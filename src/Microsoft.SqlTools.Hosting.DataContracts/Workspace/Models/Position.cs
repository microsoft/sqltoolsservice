using System.Diagnostics;

namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models
{
    [DebuggerDisplay("Position = {Line}:{Character}")]
    public class Position
    {
        /// <summary>
        /// Gets or sets the zero-based line number.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Gets or sets the zero-based column number.
        /// </summary>
        public int Character { get; set; }

        /// <summary>
        /// Overrides the base equality method
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null ||  (obj as Position == null))
            {
                return false;
            }
            Position p = (Position) obj;
            bool result = (Line == p.Line) && (Character == p.Character);
            return result;
        }


        /// <summary>
        /// Overrides the base GetHashCode method
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Line.GetHashCode();
            hash = hash * 23 + Character.GetHashCode();
            return hash;
        }
    }
}