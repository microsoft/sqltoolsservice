//
// Code originally from http://credentialmanagement.codeplex.com/, 
// Licensed under the Apache License 2.0 
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Credentials.Win32
{
    public class CredentialSet: List<Win32Credential>, IDisposable
    {
        bool _disposed;

        public CredentialSet()
        {
        }

        public CredentialSet(string target)
            : this()
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException(nameof(target));
            }
            Target = target;
        }

        public string Target { get; set; }


        public void Dispose()
        {
            Dispose(true);

            // Prevent GC Collection since we have already disposed of this object
            GC.SuppressFinalize(this);
        }

        ~CredentialSet()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (Count > 0)
                    {
                        ForEach(cred => cred.Dispose());
                    }
                }
            }
            _disposed = true;
        }

        public CredentialSet Load()
        {
            LoadInternal();
            return this;
        }

        private void LoadInternal()
        {
            IntPtr pCredentials = IntPtr.Zero;
            bool result = NativeMethods.CredEnumerateW(Target, 0, out uint count, out pCredentials);
            if (!result)
            {
                Logger.Error(string.Format("Win32Exception: {0}", new Win32Exception(Marshal.GetLastWin32Error()).ToString()));
                return;
            }

            // Read in all of the pointers first
            IntPtr[] ptrCredList = new IntPtr[count];
            for (int i = 0; i < count; i++)
            {
                ptrCredList[i] = Marshal.ReadIntPtr(pCredentials, IntPtr.Size*i);
            }

            // Now let's go through all of the pointers in the list
            // and create our Credential object(s)
            List<NativeMethods.CriticalCredentialHandle> credentialHandles =
                ptrCredList.Select(ptrCred => new NativeMethods.CriticalCredentialHandle(ptrCred)).ToList();

            IEnumerable<Win32Credential> existingCredentials = credentialHandles
                .Select(handle => handle.GetCredential())
                .Select(nativeCredential =>
                            {
                                Win32Credential credential = new Win32Credential();
                                credential.LoadInternal(nativeCredential);
                                return credential;
                            });
            AddRange(existingCredentials);

            // The individual credentials should not be free'd
            credentialHandles.ForEach(handle => handle.SetHandleAsInvalid());

            // Clean up memory to the Enumeration pointer
            NativeMethods.CredFree(pCredentials);
        }

    }

}