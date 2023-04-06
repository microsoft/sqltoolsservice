//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Reflection;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// An attribute for sqlmgmt\src\permissionsdata.cs!SecurableType that maps it to the corresponding SMO
    /// type. This allows us to use that type to decide whether that securable is valid for a given server
    /// version/engine edition/engine type combo and to get the URN suffix value for that type using SMO
    /// instead of duplicating it in SqlMgmt.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class SchemaScopedSecurableAttribute : Attribute
    {
        private readonly Type _smoType;
        private readonly string _urnSuffix;
        private readonly string _additionalParam;

        /// <summary>
        /// Basic public constructor
        /// </summary>
        /// <param name="smoType">The SMO Type this Securable is mapped to</param>
        /// <param name="additionalParamName">(Optional) The name of an additional param</param>
        /// <param name="additionalParamValue">(Optional) The value of an additional param</param>
        public SchemaScopedSecurableAttribute(Type smoType, string additionalParamName = "", object additionalParamValue = null )
        {
            _smoType = smoType;
            //The additional param is optional - just ignore if we don't have a valid name
            _additionalParam = string.IsNullOrEmpty(additionalParamName)
                ? String.Empty
                : string.Format("@{0}='{1}'", additionalParamName, Urn.EscapeString(additionalParamValue.ToString()));
            PropertyInfo urnSuffixProperty = _smoType.GetProperty("UrnSuffix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) ?? throw new InvalidArgumentException(string.Format("Type {0} did not have expected UrnSuffix property defined", smoType.Name));
            _urnSuffix = urnSuffixProperty.GetValue(null, null).ToString();
        }

        /// <summary>
        /// The SMO Type that this securable is mapped to
        /// </summary>
        public Type SmoType
        {
            get { return _smoType; }
        }

        /// <summary>
        /// Whether this Securable is valid for the given server version/engine type/engine edition combo.
        /// </summary>
        /// <param name="serverVersion"></param>
        /// <param name="databaseEngineType"></param>
        /// <param name="databaseEngineEdition"></param>
        /// <returns></returns>
        public bool IsValid(ServerVersion serverVersion, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            return SmoUtility.IsSupportedObject(_smoType, serverVersion, databaseEngineType, databaseEngineEdition);
        }

        /// <summary>
        /// Builds the URN for this Securable using the specified schema name (with optional database name for db-scoped securables)
        /// </summary>
        /// <param name="schemaName"></param>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public string GetUrn(string schemaName, string databaseName = "")
        {
            StringBuilder urn = new StringBuilder("Server");
            if (!string.IsNullOrEmpty(databaseName))
            {
                urn.AppendFormat("/Database[@Name='{0}']", Urn.EscapeString(databaseName));
            }
            urn.AppendFormat("/{0}[{1}{2}@Schema='{3}']",
                _urnSuffix,
                _additionalParam,
                string.IsNullOrEmpty(_additionalParam) ? string.Empty : " and ",
                Urn.EscapeString(schemaName));
            return urn.ToString();
        }
    }
}
