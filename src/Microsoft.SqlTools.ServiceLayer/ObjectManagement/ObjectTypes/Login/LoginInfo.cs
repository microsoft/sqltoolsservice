//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LoginAuthenticationType
    {
        [EnumMember(Value = "Windows")]
        Windows,
        [EnumMember(Value = "Sql")]
        Sql,
        [EnumMember(Value = "AAD")]
        AAD,
        [EnumMember(Value = "Others")]
        Others
    }

    public class ServerLoginDatabaseUserMapping
    {
        public string Database { get; set; }
        public string User { get; set; }
        public string DefaultSchema { get; set; }
        public string[] DatabaseRoles { get; set; }
    }

    /// <summary>
    /// a class for storing various login properties
    /// </summary>
    public class LoginInfo : SecurityPrincipalObject
    {
        public LoginAuthenticationType AuthenticationType { get; set; }

        public bool WindowsGrantAccess { get; set; }

        public bool MustChangePassword { get; set; }

        public bool IsEnabled { get; set; }
        public bool ConnectPermission { get; set; }

        public bool IsLockedOut { get; set; }

        public bool EnforcePasswordPolicy { get; set; }

        public bool EnforcePasswordExpiration { get; set; }

        public string Password { get; set; }

        public string OldPassword { get; set; }

        public string DefaultLanguage { get; set; }

        public string DefaultDatabase { get; set; }

        public string[] ServerRoles { get; set; }

        public ServerLoginDatabaseUserMapping[] UserMapping;
    }
}
