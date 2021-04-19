using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;

namespace Microsoft.AzureMonitor.ServiceLayer.DataSource
{
    public static class DataSourceExtensions
    {
        public static void AddToValueList(this Dictionary<string, List<NodeInfo>> dictionary, string key, NodeInfo node)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key].Add(node);
            }
            else
            {
                dictionary[key] = new List<NodeInfo> {node};
            }
        }
    }
}