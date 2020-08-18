// <copyright file="SystemExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>

namespace  Microsoft.Kusto.ServiceLayer.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    /// <summary>
    /// Represents .NET system extensions.
    /// </summary>
    public static class SystemExtensions
    {
        /// <summary>
        /// Removes a prefix from a string.  If the string does not begin with the prefix, returns the string as-is.
        /// </summary>
        /// <param name="value">The string</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns>The value without the prefix.</returns>
        public static string TrimPrefix(this string value, string prefix)
        {
            return value == null ? null
                : value.StartsWith(prefix, StringComparison.Ordinal) ? value.Substring(prefix.Length)
                : value;
        }

        /// <summary>
        /// Efficiently realizes an enumerable as a collection in memory.  Can be used to avoid multiple iterations.
        /// </summary>
        /// <typeparam name="T">The enumerated type.</typeparam>
        /// <param name="enumerable">The enumerable to materialize.</param>
        /// <returns>The materialized enumerable, for example an array or list.</returns>
        public static IEnumerable<T> Materialize<T>(this IEnumerable<T> enumerable)
        {
            // Return lists and arrays as-is.  Convert other enumerables to an array, for example iterator methods and groupings.
            enumerable = enumerable ?? Enumerable.Empty<T>();
            return enumerable as List<T>
                ?? enumerable as Array as IEnumerable<T>
                ?? enumerable.ToArray();
        }

        /// <summary>
        /// Enumerates the records in an <see cref="IDataReader"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The enumerable.</returns>
        public static IEnumerable<IDataRecord> ToEnumerable(this IDataReader reader)
        {
            ValidationUtils.IsArgumentNotNull(reader, nameof(reader));

            while (reader.Read())
            {
                yield return reader;
            }
        }

        /// <summary>
        /// Gets a scalar result from an <see cref="IDataReader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns>The result.</returns>
        public static T ToScalar<T>(this IDataReader reader)
        {
            if (!reader.Read())
            {
                throw new InvalidOperationException("The query returned no results.");
            }

            var value = reader[0];
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}