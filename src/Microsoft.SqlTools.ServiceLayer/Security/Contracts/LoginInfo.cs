//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{
    /// <summary>
    /// a class for storing various login properties
    /// </summary>
    public class LoginInfo
    {
//             // General data
//             private ServerRoles         serverRoles             = null;
//             private HybridDictionary    databaseRolesCollection = null;
//             private static string       defaultLanguageDisplay;
//             private StringCollection credentials = null;

        public string LoginName { get; set; }

        public AuthType LoginType { get; set; }

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
