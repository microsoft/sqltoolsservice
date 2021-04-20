using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class FindNodesResponse
    {
        /// <summary>
        /// Information describing the matching nodes in the tree
        /// </summary>
        public List<NodeInfo> Nodes { get; set; }
    }
}