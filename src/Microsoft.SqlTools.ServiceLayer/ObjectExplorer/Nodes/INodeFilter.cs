//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

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

        public static string GetPropertyFilter(IEnumerable<INodeFilter> filters, Type type, ValidForFlag validForFlag)
        {
            string filter = "";
            var list = filters.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];

                var propertyFilterString = value.ToPropertyFilterString(type, validForFlag);
                if (propertyFilterString != string.Empty)
                {
                    string andPrefix = filter == string.Empty ? string.Empty : "and";
                    filter = $"{filter} {andPrefix} {propertyFilterString}";
                }
            }

            if (filter != string.Empty)
            {
                filter = $"[{filter}]";
            }
            return filter;
        }

        public static string ConcatProperties(IEnumerable<INodeFilter> filters, Type type, ValidForFlag validForFlag)
        {
            string filter = "";
            var list = filters.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];

                string andPrefix = filter == string.Empty ? string.Empty : " and ";
                var filterString = value.ToPropertyFilterString(type, validForFlag);
                if (filterString != string.Empty) {
                    filter = $"{filter}{andPrefix}{filterString}";
                }
            }
            return filter == string.Empty ? string.Empty : $"[{filter}]";
        }
    }
}
