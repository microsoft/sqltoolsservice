//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Credentials.Linux
{
    /// <summary>
    /// Store configuration struct
    /// </summary>
    internal struct StoreConfig
    {
        public string CredentialFolder { get; set; }
        public string CredentialFile { get; set; }
        public bool IsRelativeToUserHomeDir { get; set; }
    }
    
#if !WINDOWS_ONLY_BUILD

    /// <summary>
    /// Linux implementation of the credential store.
    /// 
    /// <remarks>
    /// This entire implementation may need to be revised to support encryption of
    /// passwords and protection of them when loaded into memory.
    /// </remarks>
    /// </summary>
    internal class LinuxCredentialStore : ICredentialStore
    {
        private string credentialFolderPath;
        private string credentialFileName;
        private FileTokenStorage storage;

        public LinuxCredentialStore(StoreConfig config)
        {
            Validate.IsNotNull("config", config);
            Validate.IsNotNullOrEmptyString("credentialFolder", config.CredentialFolder);
            Validate.IsNotNullOrEmptyString("credentialFileName", config.CredentialFile);
            
            this.credentialFolderPath = config.IsRelativeToUserHomeDir ? GetUserScopedDirectory(config.CredentialFolder) : config.CredentialFolder;
            this.credentialFileName = config.CredentialFile;


            string combinedPath = Path.Combine(this.credentialFolderPath, this.credentialFileName);
            storage = new FileTokenStorage(combinedPath);
        }
        
        public bool DeletePassword(string credentialId)
        {
            Validate.IsNotNullOrEmptyString("credentialId", credentialId);
            IEnumerable<Credential> creds;
            if (LoadCredentialsAndFilterById(credentialId, out creds))
            {
                storage.SaveEntries(creds);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets filtered credentials with a specific ID filtered out
        /// </summary>
        /// <returns>True if the credential to filter was removed, false if it was not found</returns>
        private bool LoadCredentialsAndFilterById(string idToFilter, out IEnumerable<Credential> creds)
        {
            bool didRemove = false;
            creds = storage.LoadEntries().Where(cred =>
            {
                if (IsCredentialMatch(idToFilter, cred))
                {
                    didRemove = true;
                    return false; // filter this out
                }
                return true;
            }).ToList();    // Call ToList ensures Where clause is executed so didRemove can be evaluated

            return didRemove;
        }

        private static bool IsCredentialMatch(string credentialId, Credential cred)
        {
            return string.Equals(credentialId, cred.CredentialId, StringComparison.Ordinal);
        }

        public bool TryGetPassword(string credentialId, out string password)
        {
            Validate.IsNotNullOrEmptyString("credentialId", credentialId);
            Credential cred = storage.LoadEntries().FirstOrDefault(c => IsCredentialMatch(credentialId, c));
            if (cred != null)
            {
                password = cred.Password;
                return true;
            }

            // Else this was not found in the list
            password = null;
            return false;
        }

        public bool Save(Credential credential)
        {
            Credential.ValidateForSave(credential);

            // Load the credentials, removing the existing Cred for this 
            IEnumerable<Credential> creds;
            LoadCredentialsAndFilterById(credential.CredentialId, out creds);
            storage.SaveEntries(creds.Append(credential));
            
            return true;
        }


        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal string CredentialFolderPath
        {
            get { return this.credentialFolderPath; }
        }

        /// <summary>
        /// Concatenates a directory to the user home directory's path
        /// </summary>
        internal static string GetUserScopedDirectory(string userPath)
        {
            string homeDir = GetHomeDirectory() ?? string.Empty;
            return Path.Combine(homeDir, userPath);
        }


        /// <summary>Gets the current user's home directory.</summary>
        /// <returns>The path to the home directory, or null if it could not be determined.</returns>
        internal static string GetHomeDirectory()
        {
            // First try to get the user's home directory from the HOME environment variable.
            // This should work in most cases.
            string userHomeDirectory = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(userHomeDirectory))
            {
                return userHomeDirectory;
            }
            
            // In initialization conditions, however, the "HOME" environment variable may 
            // not yet be set. For such cases, consult with the password entry.
            
            // First try with a buffer that should suffice for 99% of cases.
            // Note that, theoretically, userHomeDirectory may be null in the success case 
            // if we simply couldn't find a home directory for the current user.
            // In that case, we pass back the null value and let the caller decide
            // what to do.
            return GetHomeDirectoryFromPw();
        }

        internal static string GetHomeDirectoryFromPw()
        {
            string userHomeDirectory = null;
            const int BufLen = 1024;
            if (TryGetHomeDirectoryFromPasswd(BufLen, out userHomeDirectory))
            {
                return userHomeDirectory;
            }
            // Fallback to heap allocations if necessary, growing the buffer until
            // we succeed.  TryGetHomeDirectory will throw if there's an unexpected error.
            int lastBufLen = BufLen;
            while (true)
            {
                lastBufLen *= 2;
                if (TryGetHomeDirectoryFromPasswd(lastBufLen, out userHomeDirectory))
                {
                    return userHomeDirectory;
                }
            }   
        }

        /// <summary>Wrapper for getpwuid_r.</summary>
        /// <param name="bufLen">The length of the buffer to use when storing the password result.</param>
        /// <param name="path">The resulting path; null if the user didn't have an entry.</param>
        /// <returns>true if the call was successful (path may still be null); false is a larger buffer is needed.</returns>
        private static bool TryGetHomeDirectoryFromPasswd(int bufLen, out string path)
        {
            // Call getpwuid_r to get the passwd struct
            Interop.Sys.Passwd passwd;
            IntPtr buffer = Marshal.AllocHGlobal(bufLen);
            try
            {
                int error = Interop.Sys.GetPwUidR(Interop.Sys.GetEUid(), out passwd, buffer, bufLen);

                // If the call succeeds, give back the home directory path retrieved
                if (error == 0)
                {
                    Debug.Assert(passwd.HomeDirectory != IntPtr.Zero);
                    path = Marshal.PtrToStringAnsi(passwd.HomeDirectory);
                    return true;
                }

                // If the current user's entry could not be found, give back null
                // path, but still return true as false indicates the buffer was
                // too small.
                if (error == -1)
                {
                    path = null;
                    return true;
                }

                var errorInfo = new Interop.ErrorInfo(error);

                // If the call failed because the buffer was too small, return false to 
                // indicate the caller should try again with a larger buffer.
                if (errorInfo.Error == Interop.Error.ERANGE)
                {
                    path = null;
                    return false;
                }

                // Otherwise, fail.
                throw new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
            }
            finally
            {
                // Deallocate the buffer we created
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
#endif

}
