//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DatabaseUserType
    {
        [EnumMember(Value = "UserWithLogin")]
        UserWithLogin,
        [EnumMember(Value = "UserWithoutLogin")]
        UserWithoutLogin
    }

    public class ExtendedProperty 
    {

        public string? Name { get; set; }

        public string? Value { get; set; }
    }

    public class SqlObject
    {
        public string? Name { get; set; }

        public string? Path { get; set; }
    }

    public class Permission
    {
        public string? Name { get; set; }

        public bool Grant { get; set; }

        public bool WithGrant { get; set; }

        public bool Deny { get; set; }
    }

    public class SecurablePermissions
    {
        public SqlObject? Securable { get; set; }

        public Permission[]? Permissions { get; set; }
    }

    /// <summary>
    /// a class for storing various user properties
    /// </summary>
    public class UserInfo
    {
        public DatabaseUserType? Type { get; set; }

        public string? UserName { get; set; }

        public string? LoginName { get; set; }

        public string? Password { get; set; }

        public string? DefaultSchema { get; set; }

        public string[]? OwnedSchemas { get; set; }

        public bool isEnabled { get; set; }

        public bool isAAD { get; set; }

        public ExtendedProperty[]? ExtendedProperties { get; set; }

        public SecurablePermissions[]? SecurablePermissions { get; set; }   
    }

    /// <summary>
    /// The information required to render the user view.
    /// </summary>
    public class UserViewInfo
    {
        public UserInfo? ObjectInfo { get; set; }

        public bool SupportContainedUser { get; set; }

        public bool SupportWindowsAuthentication { get; set; }

        public bool SupportAADAuthentication { get; set; }

        public bool SupportSQLAuthentication { get; set; }

        public string[]? Languages { get; set; }

        public string[]? Schemas { get; set; }

        public string[]? Logins { get; set; }

        public string[]? DatabaseRoles { get; set; }
    }
}
