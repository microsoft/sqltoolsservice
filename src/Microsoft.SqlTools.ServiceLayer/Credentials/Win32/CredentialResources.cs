//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Credentials.Win32
{
    // TODO Replace this strings class with a resx file
    internal class CredentialResources
    {
        public const string PasswordLengthExceeded = "The password has exceeded 512 bytes.";
        public const string TargetRequiredForDelete = "Target must be specified to delete a credential.";
        public const string TargetRequiredForLookup = "Target must be specified to check existance of a credential.";
        public const string CredentialDisposed = "Win32Credential object is already disposed.";
    }
}
