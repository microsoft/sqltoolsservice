// <copyright file="KustoQueryUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>
using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public static class KustoQueryUtils
    {
        public const string StatementSeparator = "\n| "; // Start each statement on a new line. Not required by Kusto, but doing this for readability of scripts generated from here.

        /// <summary>
        /// Escape table/column/database names for a Kusto query.
        /// </summary>
        /// <param name="name">The name to be escaped</param>
        /// <param name="alwaysEscape">Always escape if this flag is set</param>
        /// <returns>The escaped string</returns>
        public static string EscapeName(string name, bool alwaysEscape = false)
        {
            if (name.StartsWith("[@") || name == "*") // Field already escaped. No escaping required for '*' operand
            { 
                return name;
            } 

            string result = name;
            Regex  rx = new Regex("[^_a-zA-Z0-9]");
            string [] kustoKeywordList = {"and", "anomalychart", "areachart", "asc", "barchart", "between", "bool", "boolean", "by",
                "columnchart", "consume", "contains", "containscs", "count", "date", "datetime", "default", "desc", "distinct",
                "double", "dynamic", "endswith", "evaluate", "extend", "false", "filter", "find", "first", "flags", "float",
                "getschema", "has", "hasprefix", "hassuffix", "in", "int", "join", "journal", "kind", "ladderchart", "last",
                "like", "limit", "linechart", "long", "materialize", "mvexpand", "notcontains", "notlike", "of", "or", "order",
                "parse", "piechart", "pivotchart", "print", "project", "queries", "real", "regex", "sample", "scatterchart",
                "search", "set", "sort", "stacked", "stacked100", "stackedareachart", "startswith", "string", "summarize",
                "take", "time", "timechart", "timeline", "timepivot", "timespan", "to", "top", "toscalar", "true", "union", 
                "unstacked", "viewers", "where", "withsource"}; // add more keywords here

            var escapeName = rx.IsMatch(name) || kustoKeywordList.Any(name.Contains) || alwaysEscape;
            if (escapeName) 
            {
                if (name.IndexOf('"') > -1) 
                {
                    result = "[@'" + name + "']";
                }
                else 
                {
                    result = "[@\"" + name + "\"]";
                }
            }

            return result;
        }

    }
}
