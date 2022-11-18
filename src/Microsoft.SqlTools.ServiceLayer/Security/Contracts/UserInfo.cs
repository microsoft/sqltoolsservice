//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{
    public enum AuthType
    {
        WindowsAuth = 0,
        SqlLogin = 1,
        AzureActiveDirectory = 2
    }

    /// <summary>
    /// a class for storing various user properties
    /// </summary>
    public class UserInfo
    {
        public string LoginName { get; set; }

        public AuthType Type { get; set; }

        public string Password { get; set; }

        public string OldPassword { get; set; }
        
        public bool EnforcePasswordPolicy { get; set; }

        public bool EnforcePasswordExpiration { get; set; }

        public bool UserMustChangePassword { get; set; }

        public string DefaultDatabase { get; set; }

        public string DefaultLanguage { get; set; }

        public string[] ServerRoles { get; set; }

        public string[] UserMapping { get; set; }     
    }
}
