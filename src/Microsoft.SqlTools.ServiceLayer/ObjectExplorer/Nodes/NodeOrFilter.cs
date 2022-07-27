//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes
{
    /// <summary>
    /// Has information for filtering a SMO object by properties 
    /// </summary>
    public class NodeOrFilter : NodePropertyFilter
    {
        /// <summary>
        /// Filter values
        /// </summary>
        public List<NodePropertyFilter> FilterList { get; set; }

        /// <summary>
        /// Creates a string representation of a node "or" filter, which is combined in the INodeFilter interface to construct the filter used in the URN to query the SQL objects.
        /// Example of the output: (@ TableTemporalType = 2 or @ LedgerTableType = 2)
        /// </summary>
        /// <returns></returns>
        new public string ToPropertyFilterString(Type type, ValidForFlag validForFlag)
        {
            string filter = "";
            for (int i = 0; i < FilterList.Count; i++)
            {
                var nodeFilter = FilterList[i];
                string orPrefix = i == 0 ? string.Empty : "or";
                filter = $"{filter} {orPrefix} {nodeFilter.ToPropertyFilterString(type, validForFlag)}";
            }
            if (filter != string.Empty)
            {
                filter = $"({filter})";
            }
            return filter;
        }
    }
}
