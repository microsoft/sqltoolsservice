//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Utilities
{
    internal static class NullableUtils
    {
        /// <summary>
        /// Determines whether the type is <see cref="Nullable{T}"/>.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>
        ///   <c>true</c> if <paramref name="t"/> is <see cref="Nullable{T}"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullable(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Unwraps the <see cref="Nullable{T}"/> if necessary and returns the underlying value type.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>The underlying value type the <see cref="Nullable{T}"/> type was produced from,
        /// or the <paramref name="t"/> type if the type is not <see cref="Nullable{T}"/>.
        /// </returns>
        public static Type GetUnderlyingTypeIfNullable(Type t)
        {
            return IsNullable(t) ? Nullable.GetUnderlyingType(t) : t;
        }
    }
}