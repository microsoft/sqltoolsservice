// <copyright file="KustoQueryUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public static class KustoQueryUtils
    {
        public const string StatementSeparator = "\n | "; // Start each statement on a new line. Not required by Kusto, but doing this for readability of scripts generated from here.

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

        
        public static bool  IsClusterLevelQuery(string query) 
        {
            string [] clusterLevelQueryPrefixes = {
                ".show databases",
                ".show schema"
            };

            return clusterLevelQueryPrefixes.Any(query.StartsWith);
        }
        
        /// <summary>
        /// Adds an object of type DataSourceObjectMetadata to a dictionary<string, Dictionary<string, T>>. If the key exists then the item is added
        /// to the list. If not then the key is created and then added.
        /// </summary>
        /// <param name="dictionary">The dictionary of the dictionary that the list should be added to.</param>
        /// <param name="key">The key to be added.</param>
        /// <param name="metadata">The metadata to be added to the list.</param>
        /// <typeparam name="T"></typeparam>
        public static void SafeAdd<T>(this Dictionary<string, Dictionary<string, T>> dictionary, string key,
            T metadata) where T : DataSourceObjectMetadata
        {
            if (dictionary.ContainsKey(key))
            {
                if (dictionary[key].ContainsKey(metadata.Name))
                {
                    return;
                }
                    
                dictionary[key].Add(metadata.Name, metadata);
            }
            else
            {
                dictionary[key] = new Dictionary<string, T> {{metadata.Name, metadata}};
            }
        }
        
        /// <summary>
        /// Add a range to a dictionary of ConcurrentDictionary. Adds range to existing IEnumerable within dictionary
        /// at the same key.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="metadatas"></param>
        /// <typeparam name="T"></typeparam>
        public static void AddRange<T>(this ConcurrentDictionary<string, IEnumerable<T>> dictionary, string key,
            List<T> metadatas) where T : DataSourceObjectMetadata
        {
            if (dictionary.ContainsKey(key))
            {
                metadatas.AddRange(dictionary[key]);
            }
            
            dictionary[key] = metadatas.OrderBy(x => x.PrettyName, StringComparer.OrdinalIgnoreCase).ToList();
        }

    }
}
