using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer
{
    public class ExpandRequest
    {
        /// <summary>
        /// Returns children of a given node as a <see cref="NodeInfo"/> array.
        /// </summary>
        public static readonly
            RequestType<ExpandParams, bool> Type =
                RequestType<ExpandParams, bool>.Create("objectexplorer/expand");
    }
}