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
    public enum ServerAuthenticationType
    {
        [EnumMember(Value = "Windows")]
        Windows,
        [EnumMember(Value = "Sql")]
        Sql,
        [EnumMember(Value = "AAD")]
        AzureActiveDirectory
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DatabaseUserType
    {
        // User with a server level login.  
        [EnumMember(Value = "WithLogin")]
        WithLogin,
        // User based on a Windows user/group that has no login, but can connect to the Database Engine through membership in a Windows group.
        [EnumMember(Value = "WithWindowsGroupLogin")]
        WithWindowsGroupLogin,
        // Contained user, authentication is done within the database.
        [EnumMember(Value = "Contained")]
        Contained,
        // User that cannot authenticate.
        [EnumMember(Value = "NoConnectAccess")]
        NoConnectAccess
    }


    /// <summary>
    /// a class for storing various user properties
    /// </summary>
    public class UserInfo
    {
        public DatabaseUserType? Type { get; set; }

        public string? Name { get; set; }

        public string? LoginName { get; set; }

        public string? Password { get; set; }

        public string? DefaultSchema { get; set; }

        public string[]? OwnedSchemas { get; set; }

        public string[]? DatabaseRoles { get; set; }

        public ServerAuthenticationType AuthenticationType { get; set; }

        public string? DefaultLanguage { get; set; }
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
