//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes
{
    /// <summary>
    /// Has information for filtering a SMO object by properties 
    /// </summary>
    public class NodeFilter
    {
        /// <summary>
        /// Property name
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// Filter values
        /// </summary>
        public List<object> Values { get; set; }

        /// <summary>
        /// Type of the filter values
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Indicates which platforms a filter is valid for
        /// </summary>
        public ValidForFlag ValidFor { get; set; }

        /// <summary>
        /// The type of the Querier the filter can be applied to
        /// </summary>
        public Type TypeToReverse { get; set; }

        /// <summary>
        /// Returns true if the filter can be apply to the given type and Server type
        /// </summary>
        /// <param name="type">Type of the querier</param>
        /// <param name="validForFlag">Server Type</param>
        /// <returns></returns>
        public bool CanApplyFilter(Type type, ValidForFlag validForFlag)
        {
            bool canApplyFilter = false;
            canApplyFilter = TypeToReverse == null || TypeToReverse == type;
            canApplyFilter = canApplyFilter && (ValidFor == 0 || ValidFor.HasFlag(validForFlag));

            return canApplyFilter;
        }

        /// <summary>
        /// Creates a string from the filter property and values to be used in the Urn to query the SQL objects
        /// Example of the output:[@ IsSystemObject = 0]
        /// </summary>
        /// <returns></returns>
        public string ToPropertyFilterString()
        {
            string filter = "";
            List<object> values = Values;
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    var value = values[i];
                    object propertyValue = value;
                    if (Type == typeof(string))
                    {
                        propertyValue = $"'{propertyValue}'";
                    }
                    if (Type == typeof(Enum))
                    {
                        propertyValue = (int)Convert.ChangeType(value, Type);
                       
                    }
                    string orPrefix = i == 0 ? string.Empty : "or";
                    filter = $"{filter} {orPrefix} @{Property} = {propertyValue}";
                }
            }
            filter = $"({filter})";

            return filter;
        }

        public static string ConcatProperties(IEnumerable<NodeFilter> filters)
        {
            string filter = "";
            var list = filters.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];
                
                string andPrefix = i == 0 ? string.Empty : "and";
                filter = $"{filter} {andPrefix} {value.ToPropertyFilterString()}";
            }
            filter = $"[{filter}]";

            return filter;
        }
    }
}
