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
    public enum LoginType
    {
        [EnumMember(Value = "Windows")]
        Windows,
        [EnumMember(Value = "Sql")]
        Sql,
        [EnumMember(Value = "AAD")]
        AzureActiveDirectory
    }

    /// <summary>
    /// a class for storing various login properties
    /// </summary>
    public class LoginInfo
    {
        public string LoginName { get; set; }

        public LoginType LoginType { get; set; }

        public string CertificateName { get; set; }
            
        public string AsymmetricKeyName { get; set; }

        public bool WindowsGrantAccess { get; set; }

        public bool MustChange { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsLockedOut { get; set; }

        public bool EnforcePolicy { get; set; }

        public bool EnforceExpiration { get; set; }

        public bool WindowsAuthSupported { get; set; }

        public string Password { get; set; }

        public string OldPassword { get; set; }

        public string DefaultLanguage { get; set; }

        public string DefaultDatabase { get; set; }
    }
}
