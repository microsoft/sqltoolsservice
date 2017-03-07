//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Hosting.Contracts
{
    /// <summary>
    /// Defines the connection provider options that the DMP server
    /// implements.  This includes metadata regarding supported connection
    /// properties.
    /// </summary>
    public class ConnectionProviderOptions
    {
        public ConnectionOption[] Options { get; set; }
    }

    public class ConnectionOption
    {
        public static readonly string ValueTypeString = "string";
        public static readonly string ValueTypeMultiString = "multistring";
        public static readonly string ValueTypePassword = "password";
        public static readonly string ValueTypeNumber = "number";
        public static readonly string ValueTypeCategory = "category";
        public static readonly string ValueTypeBoolean = "boolean";

        public static readonly string SpecialValueServerName = "serverName";
        public static readonly string SpecialValueDatabaseName = "databaseName";
        public static readonly string SpecialValueAuthType = "authType";
        public static readonly string SpecialValueUserName = "userName";
        public static readonly string SpecialValuePasswordName = "password";

        public string Name { get; set; }

        public string DisplayName { get; set; }

        /// <summary>
        /// Type of the parameter.  Can be either string, number, or category.
        /// </summary>
        public string ValueType { get; set; }

        public string DefaultValue { get; set; }

        /// <summary>
        /// Set of permitted values if ValueType is category.
        /// </summary>
        public string[] CategoryValues { get; set; }

        /// <summary>
        /// Determines if the parameter is one of the 'specical' known values.
        /// Can be either Server Name, Database Name, Authentication Type,
        /// User Name, or Password
        /// </summary>
        public string SpecialValueType { get; set; }

        /// <summary>
        /// Flag to indicate that this option is part of the connection identity
        /// </summary>
        public bool IsIdentity { get; set; }

        /// <summary>
        /// Flag to indicate that this option is required
        /// </summary>
        public bool IsRequired { get; set; }   
    }
}

