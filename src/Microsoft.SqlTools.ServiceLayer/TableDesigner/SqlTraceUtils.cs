/**************************************************************
*  Copyright (C) Microsoft Corporation. All rights reserved.  *
**************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Data.Tools.Components.Diagnostics
{
    /// <summary>
    /// Provides utility methods for formatting Trace strings
    /// </summary>
    internal static class SqlTraceUtils
    {
        internal const string DefaultListStart = "{";
        internal const string DefaultListEnd = "}";
        internal const string DefaultListSeparator = ", ";
        internal const string DefaultIndent = "";
        internal const string DefaultDictionaryStart = "[";
        internal const string DefaultDictionaryEnd = "]";
        internal const string DefaultDictionaryMapsTo = " => ";

        /// <summary>
        /// Formats an enumerable collection of objects into a string, calling the converter delegate on each to get a string
        /// </summary>
        internal static string FormatEnumerable<T>(IEnumerable<T> enumerable, Func<T, string> converter, bool newlineForEach = false,
            string newlineIndent = DefaultIndent, string listStart = DefaultListStart, string listEnd = DefaultListEnd, string listSeparator = DefaultListSeparator)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(listStart);
            bool first = true;
            foreach (T t in enumerable)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    if (newlineForEach)
                    {
                        sb.AppendLine();
                        sb.Append(newlineIndent);
                    }
                    else
                    {
                        sb.Append(listSeparator);
                    }
                }

                sb.Append(converter(t));
            }
            sb.Append(listEnd);

            return sb.ToString();
        }

        /// <summary>
        /// Formats an enumerable collection of objects into a string, calling the converter delegate on each to get a string,
        /// with a name and a count of the objects prepended
        /// </summary>
        internal static string FormatNamedEnumerable<T>(string name, IEnumerable<T> enumerable, Func<T, string> converter, bool newlineForEach = false,
        string newlineIndent = DefaultIndent, string listStart = DefaultListStart, string listEnd = DefaultListEnd, string listSeparator = DefaultListSeparator)
        {
            return
                name + " (Count=" + enumerable.Count<T>() + ")=" +
                (newlineForEach ? Environment.NewLine + newlineIndent : string.Empty) +
                FormatEnumerable(enumerable, converter, newlineForEach, newlineIndent, listStart, listEnd, listSeparator);
        }

        /// <summary>
        /// Formats a dictionary of key-value pairs into a string, calling the appropriate converter delegate
        /// on each key and value to get a string
        /// </summary>
        internal static string FormatDictionary<T, U>(IDictionary<T, U> dictionary, Func<T, string> keyConverter, Func<U, string> valueConverter, bool newlineForEach = false,
            string newlineIndent = DefaultIndent, string listStart = DefaultListStart, string listEnd = DefaultListEnd, string listSeparator = DefaultListSeparator,
            string dictionaryStart = DefaultDictionaryStart, string dictionaryEnd = DefaultDictionaryEnd, string mapsTo = DefaultDictionaryMapsTo)
        {
            StringBuilder sb = new StringBuilder(listStart);
            bool first = true;
            foreach (KeyValuePair<T, U> pair in dictionary)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    if (newlineForEach)
                    {
                        sb.AppendLine();
                        sb.Append(newlineIndent);
                    }
                    else
                    {
                        sb.Append(listSeparator);
                    }
                }

                sb.Append(dictionaryStart + keyConverter(pair.Key) + mapsTo + valueConverter(pair.Value) + dictionaryEnd);
            }
            sb.AppendLine(listEnd);

            return sb.ToString();
        }

        /// <summary>
        /// Formats a dictionary of key-value pairs into a string, calling the appropriate converter delegate
        /// on each key and value to get a string with a name and a count of the objects prepended
        /// </summary>
        internal static string FormatNamedDictionary<T, U>(string name, IDictionary<T, U> dictionary, Func<T, string> keyConverter,
            Func<U, string> valueConverter, bool newlineForEach = false, string newlineIndent = DefaultIndent,
            string listStart = DefaultListStart, string listEnd = DefaultListEnd, string listSeparator = DefaultListSeparator,
            string dictionaryStart = DefaultDictionaryStart, string dictionaryEnd = DefaultDictionaryEnd, string mapsTo = DefaultDictionaryMapsTo)
        {
            return
                name + " (Count=" + dictionary.Count + ")=" +
                (newlineForEach ? Environment.NewLine + newlineIndent : string.Empty) +
                FormatDictionary(dictionary, keyConverter, valueConverter, newlineForEach, newlineIndent, listStart, listEnd, listSeparator, dictionaryStart, dictionaryEnd, mapsTo);
        }
    }
}
