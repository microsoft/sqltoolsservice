//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

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
        public string Property { get; set; } = string.Empty;

        /// <summary>
        /// Filter values
        /// </summary>
        public List<object> Values { get; set; } = default!;

        /// <summary>
        /// Type of the filter values
        /// </summary>
        public Type Type { get; set; } = default!;

        /// <summary>
        /// Indicates which platforms a filter is valid for
        /// </summary>
        public ValidForFlag ValidFor { get; set; } = ValidForFlag.None;

        /// <summary>
        /// The type of the Querier the filter can be applied to
        /// </summary>
        public Type TypeToReverse { get; set; } = default!;

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

            StringBuilder filter = new StringBuilder();
            foreach (var value in Values)
            {
                object propertyValue = value;
                if (Type == typeof(string))
                {
                    propertyValue = $"'{propertyValue}'";
                }
                else if (Type == typeof(Enum))
                {
                    propertyValue = (int)Convert.ChangeType(value, Type);
                }

                string orPrefix = filter.Length == 0 ? string.Empty : " or ";
                filter.Append($"{orPrefix}@{Property} = {propertyValue}");
            }

            if (filter.Length != 0)
            {
                return "(" + filter.ToString() + ")";
            }
            return string.Empty;
        }
    }
}
