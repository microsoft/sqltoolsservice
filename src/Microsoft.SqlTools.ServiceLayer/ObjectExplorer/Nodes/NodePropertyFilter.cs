﻿//
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
    public class NodePropertyFilter : INodeFilter
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
        /// Creates a string representation of a given node filter, which is combined in the INodeFilter interface to construct the filter used in the URN to query the SQL objects.
        /// Example of the output: (@ IsSystemObject = 0)
        /// </summary>
        /// <returns></returns>
        public string ToPropertyFilterString(Type type, ValidForFlag validForFlag)
        {
            // check first if the filter can be applied; if not just return empty string
            if (!CanApplyFilter(type, validForFlag))
            {
                return string.Empty;
            }

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

            if (filter != string.Empty)
            {
                filter = $"({filter})";
            }
            return filter;
        }
    }
}
