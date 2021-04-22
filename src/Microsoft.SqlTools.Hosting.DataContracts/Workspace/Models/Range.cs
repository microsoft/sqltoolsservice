using System.Diagnostics;

namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models
{
    [DebuggerDisplay("Start = {Start.Line}:{Start.Character}, End = {End.Line}:{End.Character}")]
    public struct Range
    {
        /// <summary>
        /// Gets or sets the starting position of the range.
        /// </summary>
        public Position Start { get; set; }

        /// <summary>
        /// Gets or sets the ending position of the range.
        /// </summary>
        public Position End { get; set; }
        
        /// <summary>
        /// Overrides the base equality method
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Range))
            {
                return false;
            }
            Range range = (Range) obj;
            bool sameStart = range.Start.Equals(Start);
            bool sameEnd = range.End.Equals(End);
            return (sameStart && sameEnd);
        }

        /// <summary>
        /// Overrides the base GetHashCode method
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Start.GetHashCode();
            hash = hash * 23 + End.GetHashCode();
            return hash;
        }
    }
}