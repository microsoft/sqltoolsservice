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

    public class CategoryValue
    {
        public string DisplayName { get; set; }

        public string Name { get; set; }
    }

    public class ConnectionOption : ServiceOption
    {
        public static readonly string SpecialValueServerName = "serverName";
        public static readonly string SpecialValueDatabaseName = "databaseName";
        public static readonly string SpecialValueAuthType = "authType";
        public static readonly string SpecialValueUserName = "userName";
        public static readonly string SpecialValuePasswordName = "password";
        public static readonly string SpecialValueAppName = "appName";

        /// <summary>
        /// Determines if the parameter is one of the 'special' known values.
        /// Can be either Server Name, Database Name, Authentication Type,
        /// User Name, or Password
        /// </summary>
        public string SpecialValueType { get; set; }

        /// <summary>
        /// Flag to indicate that this option is part of the connection identity
        /// </summary>
        public bool IsIdentity { get; set; }
    }
}

