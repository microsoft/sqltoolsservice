//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    /// <summary>
    /// Generic object-to-object mapper that uses reflection to copy property values to fields by name.
    /// Supports type conversions (including enum handling), default value skipping, and reflection result caching for performance.
    /// </summary>
    internal static class ReflectionMapper
    {
        private static readonly Dictionary<Type, PropertyInfo[]> _propCache = new Dictionary<Type, PropertyInfo[]>();
        private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new Dictionary<Type, FieldInfo[]>();

        /// <summary>
        /// Maps source object properties to destination object fields by name.
        /// </summary>
        /// <typeparam name="TSource">Source type with properties</typeparam>
        /// <typeparam name="TDestination">Destination type with fields</typeparam>
        /// <param name="source">Source object to map from</param>
        /// <param name="destinationFactory">Factory to create destination instance</param>
        /// <param name="configure">Optional action to configure destination before mapping</param>
        /// <param name="ensureFieldNames">Optional field names to ensure are set from source even if they defaulted to 0</param>
        /// <returns>Mapped destination object</returns>
        public static TDestination MapByName<TSource, TDestination>(
            TSource source,
            Func<TDestination>? destinationFactory,
            Action<TDestination>? configure = null,
            params string[]? ensureFieldNames)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var dest = destinationFactory != null ? destinationFactory() : Activator.CreateInstance<TDestination>();
            configure?.Invoke(dest);

            var srcType = typeof(TSource);
            var dstType = typeof(TDestination);

            var srcProps = GetPublicInstanceProperties(srcType);
            var dstFields = GetPublicInstanceFields(dstType);

            // Index destination fields by name (Ordinal)
            var destByName = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
            for (int i = 0; i < dstFields.Length; i++)
            {
                var f = dstFields[i];
                destByName[f.Name] = f;
            }

            for (int i = 0; i < srcProps.Length; i++)
            {
                var prop = srcProps[i];
                if (!prop.CanRead) continue;

                // Read property value; GetValue may return null
                object? value;
                try
                {
                    // Prefer the parameterless overload on modern targets
                    value = prop.GetValue(source);
                }
                catch
                {
                    continue; // unreadable; skip
                }

                if (value is null) continue;

                // Skip defaults to keep CLI clean
                if (DefaultValuePolicy.ShouldSkip(prop.PropertyType, value))
                    continue;

                // Destination must have a matching field
                if (!destByName.TryGetValue(prop.Name, out FieldInfo? destField) || destField is null)
                    continue;

                try
                {
                    // Assign directly or via enum conversion
                    if (TypeConversion.TryAssign(dest!, destField, prop.PropertyType, value))
                    {
                        continue; // assigned; next
                    }
                }
                catch
                {
                    continue; // failed assignment; skip
                }
            }

            // Safety: ensure specified fields are set from source even if destination defaulted to 0
            EnsureFields(source, dest, srcType, destByName, ensureFieldNames);

            return dest;
        }

        /// <summary>
        /// Ensures specified fields are set from source even if they defaulted to 0 in destination.
        /// Useful for enum fields that might be skipped during normal mapping.
        /// </summary>
        private static void EnsureFields<TSource, TDestination>(
            TSource source,
            TDestination dest,
            Type srcType,
            Dictionary<string, FieldInfo> destByName,
            string[]? ensureFieldNames)
        {
            if (ensureFieldNames == null || ensureFieldNames.Length == 0) return;

            foreach (var fieldName in ensureFieldNames)
            {
                if (string.IsNullOrEmpty(fieldName)) continue;

                if (destByName.TryGetValue(fieldName, out FieldInfo? ensureField) && ensureField is not null)
                {
                    object? destValue = ensureField.GetValue(dest);
                    if (destValue != null && Convert.ToInt32(destValue) == 0) // default enum value
                    {
                        var srcProp = srcType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
                        if (srcProp != null)
                        {
                            object? srcValue = null;
                            try { srcValue = srcProp.GetValue(source); } catch { /* ignore */ }
                            if (srcValue != null)
                            {
                                try { ensureField.SetValue(dest, srcValue); } catch { /* ignore */ }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets public instance properties for a type with caching to avoid repeated reflection calls.
        /// </summary>
        /// <param name="t">The type to get properties from</param>
        /// <returns>Array of public instance properties</returns>
        private static PropertyInfo[] GetPublicInstanceProperties(Type t)
        {
            if (_propCache.TryGetValue(t, out var cached)) return cached;
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _propCache[t] = props;
            return props;
        }

        /// <summary>
        /// Gets public instance fields for a type with caching to avoid repeated reflection calls.
        /// </summary>
        /// <param name="t">The type to get fields from</param>
        /// <returns>Array of public instance fields</returns>
        private static FieldInfo[] GetPublicInstanceFields(Type t)
        {
            if (_fieldCache.TryGetValue(t, out var cached)) return cached;
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            _fieldCache[t] = fields;
            return fields;
        }
    }

    /// <summary>
    /// Central policy for deciding whether a value should be skipped to keep CLI clean.
    /// </summary>
    internal static class DefaultValuePolicy
    {
        public static bool ShouldSkip(Type type, object value)
        {
            // value is not null under nullable flow because caller checks; keep defensive anyway
            if (value == null) return true;

            // Empty strings
            if (value is string s && string.IsNullOrWhiteSpace(s)) return true;

            // Empty arrays
            if (type.IsArray)
            {
                var array = value as Array;
                if (array == null || array.Length == 0) return true;
            }

            // 0 for ints
            if (type == typeof(int) && (int)value == 0) return true;

            // false for bools
            if (type == typeof(bool) && ((bool)value) == false) return true;

            // default enum (0)
            if (type.IsEnum && Convert.ToInt32(value) == 0) return true;

            return false;
        }
    }

    /// <summary>
    /// Handles compatible assignment and flexible enum conversions (int->enum, string->enum name).
    /// </summary>
    internal static class TypeConversion
    {
        public static bool TryAssign(object destinationInstance, FieldInfo destField, Type sourceType, object value)
        {
            var destType = destField.FieldType;

            // Direct assignable
            if (destType.IsAssignableFrom(sourceType))
            {
                destField.SetValue(destinationInstance, value);
                return true;
            }

            // Enum conversions
            if (destType.IsEnum)
            {
                // int -> enum
                if (sourceType == typeof(int))
                {
                    destField.SetValue(destinationInstance, Enum.ToObject(destType, (int)value));
                    return true;
                }

                // string -> enum (case-insensitive)
                if (sourceType == typeof(string))
                {
                    var s = (string)value;
                    if (!string.IsNullOrEmpty(s))
                    {
                        try
                        {
                            var parsed = Enum.Parse(destType, s, ignoreCase: true);
                            destField.SetValue(destinationInstance, parsed);
                            return true;
                        }
                        catch
                        {
                            // invalid string; fall through
                        }
                    }
                }
            }

            return false;
        }
    }
}
