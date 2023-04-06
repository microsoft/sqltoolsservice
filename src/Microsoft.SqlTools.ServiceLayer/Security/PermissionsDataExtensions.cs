//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    internal static class PermissionsDataExtensions
    {
        /// <summary>
        /// Whether this SecurableType is a valid Schema-Scoped Securable for the given server version, engine edition and engine type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="serverVersion"></param>
        /// <param name="databaseEngineEdition"></param>
        /// <param name="databaseEngineType"></param>
        /// <returns></returns>
        public static bool IsValidSchemaBoundSecurable(this SecurableType type, ServerVersion serverVersion, DatabaseEngineEdition databaseEngineEdition, DatabaseEngineType databaseEngineType)
        {
            return
                type.GetType().GetField(type.ToString())
                    .GetCustomAttributes(typeof (SchemaScopedSecurableAttribute), true)
                    .Cast<SchemaScopedSecurableAttribute>()
                    .Any(attr => attr.IsValid(serverVersion, databaseEngineType, databaseEngineEdition));
        }

        /// <summary>
        /// Gets the Schema-Scoped URN for this SecurableType
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public static string GetSchemaScopedUrn(this SecurableType type, string schema, string databaseName)
        {
            SchemaScopedSecurableAttribute attr =
                type.GetType().GetField(type.ToString())
                    .GetCustomAttributes(typeof (SchemaScopedSecurableAttribute), true)
                    .Cast<SchemaScopedSecurableAttribute>()
                    .FirstOrDefault() ?? throw new InvalidOperationException("Type {0} did not define a SchemaScopedSecurableUrn attribute");
            return attr.GetUrn(schema, databaseName);
        }
    }
}
