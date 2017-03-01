//
// Code originally from http://credentialmanagement.codeplex.com/, 
// Licensed under the Apache License 2.0 
//

namespace Microsoft.SqlTools.ServiceLayer.Credentials.Win32
{
    public enum CredentialType: uint 
    {
        None = 0,
        Generic = 1,
        DomainPassword = 2,
        DomainCertificate = 3,
        DomainVisiblePassword = 4
    }
}
