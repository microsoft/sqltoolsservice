using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer
{
    public class RefreshRequest
    {
        /// <summary>
        /// Returns children of a given node as a <see cref="NodeInfo"/> array.
        /// </summary>
        public static readonly
            RequestType<RefreshParams, bool> Type =
                RequestType<RefreshParams, bool>.Create("objectexplorer/refresh");
    }
}