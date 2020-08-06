// <copyright file="SystemExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>

using Microsoft.Kusto.ServiceLayer.DataSource;

namespace  Microsoft.Kusto.ServiceLayer.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;

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
        /// Attempts to convert a value to an enumerated value.
        /// </summary>
        /// <typeparam name="T">The enumerated type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The enumerated value if successful.  The default value otherwise.</returns>
        public static T TryConvertEnum<T>(this int value, T defaultValue = default(T))
        {
            if (Enum.IsDefined(typeof(T), value))
            {
                return (T)Enum.ToObject(typeof(T), value);
            }

            return defaultValue;
        }

        /// <summary>
        /// Attempts to parse a value as a boolean.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The parsed value.</returns>
        public static bool TryParseBool(this string value, bool defaultValue = false)
        {
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Attempts to parse a value as an enumerated value.
        /// </summary>
        /// <typeparam name="T">The enumerated type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="ignoreCase">true to ignore case; false to consider case.</param>
        /// <returns>The enumerated value if successful.  The default value otherwise.</returns>
        public static T TryParseEnum<T>(this string value, T defaultValue = default(T), bool ignoreCase = false)
            where T : struct
        {
            return Enum.TryParse(value, ignoreCase, out T result) && Enum.IsDefined(typeof(T), result) ? result : defaultValue;
        }

        /// <summary>
        /// Attempts to parse a string value as a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>The parsed value if successful; null otherwise.</returns>
        public static DateTime? TryParseDateTime(this string value)
        {
            if (!DateTime.TryParse(value, out var result))
            {
                return null;
            }

            return new DateTime(result.Ticks, DateTimeKind.Utc); // specify UTC
        }

        /// <summary>
        /// Attempts to parse a value as a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The parsed value.</returns>
        public static TimeSpan TryParseTimeSpan(this string value, TimeSpan defaultValue = default(TimeSpan))
        {
            return TimeSpan.TryParse(value, out var result) ? result : defaultValue;
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

        public static void SafeAdd(this Dictionary<string, Dictionary<string, FolderMetadata>> dictionary, string key,
            FolderMetadata folder)
        {
            if (dictionary.ContainsKey(key))
            {
                if (dictionary[key].ContainsKey(folder.Name))
                {
                    return;
                }
                    
                dictionary[key].Add(folder.Name, folder);
            }
            else
            {
                dictionary[key] = new Dictionary<string, FolderMetadata> {{folder.Name, folder}};
            }
        }

        /// <summary>
        /// Performs an action for each item of an enumerable.  This is an iterator method and must be
        /// realized in-memory, for example using <see cref="Materialize&lt;T&gt;"/>.
        /// </summary>
        /// <typeparam name="T">The enumerated type.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="action">The action.</param>
        /// <returns>The enumerable after each the action is performed.</returns>
        public static IEnumerable<T> Each<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            ValidationUtils.IsArgumentNotNull(action, nameof(action));

            foreach (var item in enumerable)
            {
                action(item);
                yield return item;
            }
        }

        /// <summary>
        /// Concatenates the members of a collection of strings, using the specified separator between each member.
        /// </summary>
        /// <param name="enumerable">A collection that contains the strings to concatenate.</param>
        /// <param name="separator">The string to use as a separator.</param>
        /// <returns>A string that consists of the members of values delimited by the separator string. If the collection is empty, the method returns <see cref="string.Empty"/>. If the collection is null, the method returns null.</returns>
        public static string Join(this IEnumerable<string> enumerable, string separator)
        {
            return enumerable == null ? null : string.Join(separator, enumerable);
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

        /// <summary>
        /// Runs an asynchronous action synchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="action">The action.</param>
        /// <returns>The result of the action.</returns>
        public static TResult RunSync<TResult>(Func<Task<TResult>> action)
        {
            try
            {
                return Task.Run(action)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (AggregateException ae)
            {
                if (ae.InnerExceptions.Count == 1)
                {
                    throw ae.InnerExceptions[0];
                }

                throw;
            }
        }
    }
}