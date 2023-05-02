//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DatabaseUserType
    {
        // Mapped to a server login.
        [EnumMember(Value = "LoginMapped")]
        LoginMapped,
        // Mapped to a Windows user or group.
        [EnumMember(Value = "WindowsUser")]
        WindowsUser,
        // Authenticate with password.
        [EnumMember(Value = "SqlAuthentication")]
        SqlAuthentication,
        // Authenticate with Azure Active Directory.
        [EnumMember(Value = "AADAuthentication")]
        AADAuthentication,
        // User that cannot authenticate.
        [EnumMember(Value = "NoLoginAccess")]
        NoLoginAccess
    }


    /// <summary>
    /// a class for storing various user properties
    /// </summary>
    public class UserInfo : SqlObject
    {
        public DatabaseUserType? Type { get; set; }

        public string? LoginName { get; set; }

        public string? Password { get; set; }

        public string? DefaultSchema { get; set; }

        public string[]? OwnedSchemas { get; set; }

        public string[]? DatabaseRoles { get; set; }

        public string? DefaultLanguage { get; set; }
    }
}
