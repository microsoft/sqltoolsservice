//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.SqlCore.Performance.Common
{
    /// <summary>
    /// Attribute to allow specifying a LocalizedString value for an enum
    /// </summary>
    internal class LocalizedStringAttribute : Attribute
    {
        public string Value { get; set; }

        public LocalizedStringAttribute(string localizedString)
        {
            if (string.IsNullOrEmpty(localizedString))
            {
                throw new ArgumentNullException("localizedString");
            }
            Value = localizedString;
        }
    }

    /// <summary>
    /// Attribute to allow specifying a Units value for an enum
    /// </summary>
    internal class UnitsAttribute : Attribute
    {
        public string Value { get; set; }

        public UnitsAttribute(string units)
        {
            if (string.IsNullOrEmpty(units))
            {
                throw new ArgumentNullException("units");
            }
            Value = units;
        }
    }

    /// <summary>
    /// Attribute to allow specifying a QueryString value for an enum. 
    /// QueryString is the prefix used in DB fields for the specified statistic/Metric.
    /// </summary>
    internal class QueryStringAttribute : Attribute
    {
        public string Value { get; set; }

        public QueryStringAttribute(string queryString)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                throw new ArgumentNullException("queryString");
            }
            Value = queryString;
        }
    }

    /// <summary>
    /// This attribute directly maps to column names in sys.query_store_runtime_stats dmv
    /// </summary>
    internal class DatabaseColumnNameAttribute : Attribute
    {
        public string Value { get; set; }

        public DatabaseColumnNameAttribute(string databaseColumnName)
        {
            if (string.IsNullOrEmpty(databaseColumnName))
            {
                throw new ArgumentNullException("databaseColumnName");
            }
            Value = databaseColumnName;
        }
    }

    /// <summary>
    /// This attribute stores a formated string that represents how this statistic(aggregation) is calculated.
    /// </summary>
    internal class RuntimeStatsCalculationSummaryAttribute : Attribute
    {
        public string Value { get; set; }

        public RuntimeStatsCalculationSummaryAttribute(string aggregationFormula)
        {
            if (string.IsNullOrEmpty(aggregationFormula))
            {
                throw new ArgumentNullException("aggregationFormula");
            }
            Value = aggregationFormula;
        }
    }

    /// <summary>
    /// This attribute stores a formated string that represents how this statistic(aggregation) is calculated.
    /// </summary>
    internal class WaitStatsCalculationSummaryAttribute : Attribute
    {
        public string Value { get; set; }

        public WaitStatsCalculationSummaryAttribute(string aggregationFormula)
        {
            if (string.IsNullOrEmpty(aggregationFormula))
            {
                throw new ArgumentNullException("aggregationFormula");
            }
            Value = aggregationFormula;
        }
    }

    /// <summary>
    /// This attribute stores a formated string that represents how this 
    /// statistic( which in an aggregation) is calculated.
    /// </summary>
    public class WaitCategoryNameAttribute : Attribute
    {
        public string Value { get; private set; }

        public WaitCategoryNameAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }
            Value = name;
        }
    }
}
