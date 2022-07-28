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
    public class NodeOrFilter : INodeFilter
    {
        /// <summary>
        /// Filter values
        /// </summary>
        public List<NodePropertyFilter> FilterList { get; set; }

        /// <summary>
        /// Returns true if any of the filters within the FilterList apply to the type and server type.
        /// Note: Not currently used, because the validity of the filter is essentially only possible while
        /// constructing the filter string, for each individual filter.
        /// </summary>
        /// <param name="type">Type of the querier</param>
        /// <param name="validForFlag">Server Type</param>
        public bool CanApplyFilter(Type type, ValidForFlag validForFlag) {
            bool canApplyFilter = false;
            foreach (var filter in FilterList) {
                canApplyFilter |= filter.CanApplyFilter(type, validForFlag);
            }
            return canApplyFilter;
        }

        /// <summary>
        /// Creates a string representation of a node "or" filter, which is combined in the INodeFilter interface to construct the filter used in the URN to query the SQL objects.
        /// Example of the output: ((@TableTemporalType = 1) or (@LedgerTableType = 1))
        /// </summary>
        /// <returns></returns>
        public string ToPropertyFilterString(Type type, ValidForFlag validForFlag)
        {
            string filter = "";
            for (int i = 0; i < FilterList.Count; i++)
            {
                var nodeFilter = FilterList[i];
                string orPrefix = filter == string.Empty ? string.Empty : " or ";

                // For "or" filter, have to check each node as it's processed for whether it's valid.
                var filterString = nodeFilter.ToPropertyFilterString(type, validForFlag);
                if (filterString != string.Empty)
                {
                    filter = $"{filter}{orPrefix}{nodeFilter.ToPropertyFilterString(type, validForFlag)}";
                }
            }
            if (filter != string.Empty)
            {
                filter = $"({filter})";
            }
            return filter;
        }
    }
}
