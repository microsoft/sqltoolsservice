//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.Management;


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
        /// Indicates if the filter is a "not" filter. Eg (not(@IsSystemObject = 0))
        /// </summary>
        public bool IsNotFilter { get; set; } = false;

        /// <summary>
        /// Indicates if the values are for type datetime
        /// </summary>
        public bool IsDateTime { get; set; } = false;

        /// <summary>
        /// Indicates the type of the filter. It can be EQUALS, DATETIME, FALSE or CONTAINS
        /// More information can be found here:
        /// https://learn.microsoft.com/en-us/sql/powershell/query-expressions-and-uniform-resource-names?view=sql-server-ver16#examples
        /// </summary>
        public FilterType FilterType { get; set; } = FilterType.EQUALS;

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
                string filterText = string.Empty;
                if (IsDateTime)
                {
                    string Value1;
                    string Value2;
                    switch (FilterType)
                    {
                        case FilterType.BETWEEN:
                            string[] betweenValues = (string[])value;
                            Value1 = DateTime.Parse((string)betweenValues[0]).ToString("yyyy-MM-dd 00:00:00.000");
                            Value2 = DateTime.Parse((string)betweenValues[1]).ToString("yyyy-MM-dd 23:59:59.999");
                            filterText = $"@{Property} >= datetime('{Value1}') and @{Property} <= datetime('{Value2}')";
                            break;
                        case FilterType.NOTBETWEEN:
                            IsNotFilter = true;
                            string[] notBetweenValues = (string[])value;
                            Value1 = DateTime.Parse((string)notBetweenValues[0]).ToString("yyyy-MM-dd 00:00:00.000");
                            Value2 = DateTime.Parse((string)notBetweenValues[1]).ToString("yyyy-MM-dd 23:59:59.999");
                            filterText = $"@{Property} >= datetime('{Value1}') and @{Property} <= datetime('{Value2}')";
                            break;
                        case FilterType.EQUALS:
                            Value1 = DateTime.Parse((string)value).ToString("yyyy-MM-dd 00:00:00.000");
                            Value2 = DateTime.Parse((string)value).ToString("yyyy-MM-dd 23:59:59.999");
                            filterText = $"@{Property} >= datetime('{Value1}') and @{Property} <= datetime('{Value2}')";
                            break;
                        case FilterType.GREATERTHAN:
                            Value1 = DateTime.Parse((string)value).ToString("yyyy-MM-dd 23:59:59.999");
                            filterText = $"@{Property} > datetime('{Value1}')";
                            break;
                        case FilterType.LESSTHAN:
                            Value1 = DateTime.Parse((string)value).ToString("yyyy-MM-dd 00:00:00.000");
                            filterText = $"@{Property} < datetime('{Value1}')";
                            break;
                        case FilterType.GREATERTHANOREQUAL:
                            Value1 = DateTime.Parse((string)value).ToString("yyyy-MM-dd 00:00:00.000");
                            filterText = $"@{Property} >= datetime('{Value1}')";
                            break;
                        case FilterType.LESSTHANOREQUAL:
                            Value1 = DateTime.Parse((string)value).ToString("yyyy-MM-dd 23:59:59.999");
                            filterText = $"@{Property} <= datetime('{Value1}')";
                            break;
                        case FilterType.NOTEQUALS:
                            IsNotFilter = true;
                            Value1 = DateTime.Parse((string)value).ToString("yyyy-MM-dd 00:00:00.000");
                            Value2 = DateTime.Parse((string)value).ToString("yyyy-MM-dd 23:59:59.999");
                            filterText = $"@{Property} >= datetime('{Value1}') and @{Property} <= datetime('{Value2}')";
                            break;
                        default:
                            break;
                    }
                }
                else if (IsNumericType(Type))
                {
                    switch (FilterType)
                    {
                        case FilterType.BETWEEN:
                            object[] betweenValues = (object[])value;
                            filterText = $"@{Property} >= {Decimal.Parse(betweenValues[0].ToString())} and @{Property} <= {Decimal.Parse(betweenValues[1].ToString())}";
                            break;
                        case FilterType.NOTBETWEEN:
                            IsNotFilter = true;
                            object[] notBetweenValues = (object[])value;
                            filterText = $"@{Property} >= {Decimal.Parse(notBetweenValues[0].ToString())} and @{Property} <= {Decimal.Parse(notBetweenValues[1].ToString())}";
                            break;
                        case FilterType.EQUALS:
                            filterText = $"@{Property} = {Decimal.Parse(value.ToString())}";
                            break;
                        case FilterType.GREATERTHAN:
                            filterText = $"@{Property} > {Decimal.Parse(value.ToString())}";
                            break;
                        case FilterType.LESSTHAN:
                            filterText = $"@{Property} < {Decimal.Parse(value.ToString())}";
                            break;
                        case FilterType.GREATERTHANOREQUAL:
                            filterText = $"@{Property} >= {Decimal.Parse(value.ToString())}";
                            break;
                        case FilterType.LESSTHANOREQUAL:
                            filterText = $"@{Property} <= {Decimal.Parse(value.ToString())}";
                            break;
                        case FilterType.NOTEQUALS:
                            filterText = $"@{Property} != {Decimal.Parse(value.ToString())}";
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    object propertyValue = value;
                    if (Type == typeof(string))
                    {

                        var escapedString = CUtils.EscapeStringSQuote(propertyValue.ToString());
                        if (this.FilterType == FilterType.STARTSWITH || this.FilterType == FilterType.ENDSWITH)
                        {
                            var regex = new Regex(@"%|_|\[|\]|\^");
                            escapedString = regex.Replace(escapedString, "[$0]");
                            if (this.FilterType == FilterType.STARTSWITH)
                            {
                                propertyValue = $"'{escapedString}%'";
                            }
                            else if (this.FilterType == FilterType.ENDSWITH)
                            {
                                propertyValue = $"'%{escapedString}'";
                            }
                        }
                        else
                        {
                            propertyValue = $"'{escapedString}'";
                        }
                    }
                    else if (Type == typeof(Enum))
                    {
                        propertyValue = (int)Convert.ChangeType(value, Type);
                    }
                    switch (FilterType)
                    {
                        case FilterType.EQUALS:
                            filterText = $"@{Property} = {propertyValue}";
                            break;
                        case FilterType.NOTEQUALS:
                            filterText = $"@{Property} != {propertyValue}";
                            break;
                        case FilterType.LESSTHAN:
                            filterText = $"@{Property} < {propertyValue}";
                            break;
                        case FilterType.GREATERTHAN:
                            filterText = $"@{Property} > {propertyValue}";
                            break;
                        case FilterType.LESSTHANOREQUAL:
                            filterText = $"@{Property} <= {propertyValue}";
                            break;
                        case FilterType.GREATERTHANOREQUAL:
                            filterText = $"@{Property} >= {propertyValue}";
                            break;
                        case FilterType.DATETIME:
                            filterText = $"@{Property} = datetime({propertyValue})";
                            break;
                        case FilterType.FALSE:
                            filterText = $"@{Property} = false()";
                            break;
                        case FilterType.ISNULL:
                            filterText = $"isnull(@{Property})";
                            break;
                        case FilterType.CONTAINS:
                            filterText = $"contains(@{Property}, {propertyValue})";
                            break;
                        case FilterType.STARTSWITH:
                        case FilterType.ENDSWITH:
                            filterText = $"like(@{Property}, {propertyValue})";
                            break;
                    }
                }

                string orPrefix = filter.Length == 0 ? string.Empty : " or ";
                if (IsNotFilter)
                {
                    filter.Append($"{orPrefix}not({filterText})");
                }
                else
                {
                    filter.Append($"{orPrefix}{filterText}");
                }
            }

            if (filter.Length != 0)
            {
                return "(" + filter.ToString() + ")";
            }
            return string.Empty;
        }

        public static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }

    public enum FilterType
    {
        EQUALS,
        DATETIME,
        FALSE,
        ISNULL,
        NOTEQUALS,
        LESSTHAN,
        GREATERTHAN,
        LESSTHANOREQUAL,
        GREATERTHANOREQUAL,
        BETWEEN,
        NOTBETWEEN,
        CONTAINS,
        STARTSWITH,
        ENDSWITH
    }
}
