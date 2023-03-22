//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.SqlTools.Credentials.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Credentials.OSX
{

#if !WINDOWS_ONLY_BUILD

    /// <summary>
    /// OSX implementation of the credential store
    /// </summary>
    internal class OSXCredentialStore : ICredentialStore
    {
        public bool DeletePassword(string credentialId)
        {
            Validate.IsNotNullOrEmptyString("credentialId", credentialId);
            return DeletePasswordImpl(credentialId);
        }

        public bool TryGetPassword(string credentialId, out string? password)
        {
            Validate.IsNotNullOrEmptyString("credentialId", credentialId);
            return FindPassword(credentialId, out password);
        }

        public bool Save(Credential credential)
        {
            Credential.ValidateForSave(credential);
            bool result = false;

            // Note: OSX blocks AddPassword if the credential 
            // already exists, so for now we delete the password if already present since we're updating
            // the value. In the future, we could consider updating but it's low value to solve this
            DeletePasswordImpl(credential.CredentialId);

            // Now add the password
            result = AddGenericPassword(credential);
            return result;
        }

        private bool AddGenericPassword(Credential credential)
        {
            IntPtr passwordPtr = Marshal.StringToCoTaskMemUTF8(credential.Password);
            Interop.Security.OSStatus status = Interop.Security.SecKeychainAddGenericPassword(
              IntPtr.Zero,
              InteropUtils.GetLengthInBytes(credential.CredentialId, Encoding.UTF8),
              credential.CredentialId,
              0,
              null,
              InteropUtils.GetLengthInBytes(credential.Password, Encoding.UTF8),
              passwordPtr,
              IntPtr.Zero);

            return status == Interop.Security.OSStatus.ErrSecSuccess;
        }

        /// <summary>
        /// Finds the first password matching this credential
        /// </summary>
        private bool FindPassword(string credentialId, out string? password)
        {
            password = null;
            using (KeyChainItemHandle? handle = LookupKeyChainItem(credentialId))
            {
                if (handle == null)
                {
                    return false;
                }
                password = handle.Password;
            }

            return true;
        }

        private KeyChainItemHandle? LookupKeyChainItem(string credentialId)
        {
            UInt32 passwordLength;
            IntPtr passwordPtr;
            IntPtr item;
            Interop.Security.OSStatus status = Interop.Security.SecKeychainFindGenericPassword(
                IntPtr.Zero,
                InteropUtils.GetLengthInBytes(credentialId, Encoding.UTF8),
                credentialId,
                0,
                null,
                out passwordLength,
                out passwordPtr,
                out item);

            if (status == Interop.Security.OSStatus.ErrSecSuccess)
            {
                return new KeyChainItemHandle(item, passwordPtr, passwordLength, Encoding.UTF8);
            }
            else
            {
#pragma warning disable 0612
                // Intentional fallback to Unicode to retrieve old passwords before encoding shift
                status = Interop.SecurityOld.SecKeychainFindGenericPassword(
                    IntPtr.Zero,
                    InteropUtils.GetLengthInBytes(credentialId, Encoding.Unicode),
                    credentialId,
                    0,
                    null!,
                    out passwordLength,
                    out passwordPtr,
                    out item);
#pragma warning restore 0612
                if (status == Interop.Security.OSStatus.ErrSecSuccess)
                {
                    using var handle = new KeyChainItemHandle(item, passwordPtr, passwordLength, Encoding.Unicode);
                    // Migrate credential to 'Auto' encoding.
                    if (handle != null)
                    {
                        var saveResult = this.AddGenericPassword(credential: new Credential(credentialId, handle.Password));
                        if (saveResult)
                        {
                            // Safe to delete old password now.
                            Interop.Security.SecKeychainItemDelete(handle);
                        }
                        // Lookup keychain again to fetch handle for new credential.
                        return LookupKeyChainItem(credentialId);
                    }
                }
            }
            return null;
        }

        private bool DeletePasswordImpl(string credentialId)
        {
            // Find password, then Delete, then cleanup
            using (KeyChainItemHandle? handle = LookupKeyChainItem(credentialId))
            {
                if (handle == null)
                {
                    return false;
                }
                Interop.Security.OSStatus status = Interop.Security.SecKeychainItemDelete(handle);
                return status == Interop.Security.OSStatus.ErrSecSuccess;
            }
        }

        private class KeyChainItemHandle : SafeCreateHandle
        {
            private IntPtr passwordPtr;
            private int passwordLength;
            private Encoding encoding = Encoding.UTF8;

            public KeyChainItemHandle() : base()
            {

            }

            public KeyChainItemHandle(IntPtr itemPtr) : this(itemPtr, IntPtr.Zero, 0, Encoding.UTF8)
            {

            }

            public KeyChainItemHandle(IntPtr itemPtr, IntPtr passwordPtr, UInt32 passwordLength, Encoding encoding)
                : base(itemPtr)
            {
                if (encoding != Encoding.UTF8 && encoding != Encoding.Unicode)
                {
                    throw new ArgumentException($"Encoding {encoding} not supported.");
                }
                this.passwordPtr = passwordPtr;
                this.passwordLength = (int)passwordLength;
                this.encoding = encoding;
            }

            public string? Password
            {
                get
                {
                    if (IsInvalid)
                    {
                        return null;
                    }
                    return InteropUtils.CopyToString(passwordPtr, passwordLength,
                        this.encoding == Encoding.UTF8 ? Encoding.UTF8: Encoding.Unicode);
                }
            }
            protected override bool ReleaseHandle()
            {
                if (passwordPtr != IntPtr.Zero)
                {
                    Interop.Security.SecKeychainItemFreeContent(IntPtr.Zero, passwordPtr);
                }
                base.ReleaseHandle();
                return true;
            }
        }
    }

#endif

}

