//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes
{
    /// <summary>
    /// Has information for filtering a SMO object by properties 
    /// </summary>
    public interface INodeFilter
    {
        /// <summary>
        /// Returns true if the filter can be apply to the given type and Server type
        /// </summary>
        /// <param name="type">Type of the querier</param>
        /// <param name="validForFlag">Server Type</param>
        /// <returns></returns>
        bool CanApplyFilter(Type type, ValidForFlag validForFlag);

        /// <summary>
        /// Creates a string from the filter property and values to be used in the Urn to query the SQL objects
        /// Example of the output:[@ IsSystemObject = 0]
        /// </summary>
        /// <returns></returns>
        string ToPropertyFilterString(Type type, ValidForFlag validForFlag);

        /// <summary>
        /// Creates a fully paramaterized property filter string for the URN query for SQL objects.
        /// Example of the output:[@ IsSystemObject = 0]
        /// </summary>
        /// <returns></returns>
        public static string GetPropertyFilter(IEnumerable<INodeFilter> filters, Type type, ValidForFlag validForFlag)
        {
            StringBuilder filter = new StringBuilder();
            foreach (var value in filters)
            {
                string andPrefix = filter.Length == 0 ? string.Empty : " and ";
                var filterString = value.ToPropertyFilterString(type, validForFlag);
                if (filterString != string.Empty) {
                    filter.Append($"{andPrefix}{filterString}");
                }
            }

            if (filter.Length != 0)
            {
                return "[" + filter.ToString() + "]";
            }
            return string.Empty;
        }

        public static string AddPropertyFilterToFilterString(string filterString, IEnumerable<INodeFilter> filters, Type type, ValidForFlag validForFlag)
        {
            if(String.IsNullOrEmpty(filterString))
            {
                return GetPropertyFilter(filters, type, validForFlag);
            }
            foreach(var value in filters)
            {
                var filter = value.ToPropertyFilterString(type, validForFlag);
                if(filter != string.Empty)
                {
                    filterString = filterString.Remove(filterString.Length - 1, 1) + $" and {filter}" + "]";
                }
            }
            return filterString;
        }
    }
}
