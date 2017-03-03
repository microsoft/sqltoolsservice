//
// Code originally from http://credentialmanagement.codeplex.com/, 
// Licensed under the Apache License 2.0 
//

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.SqlTools.Credentials.Win32
{
    public class Win32Credential: IDisposable
    {
        bool disposed;

        CredentialType type;
        string target;
        SecureString password;
        string username;
        string description;
        DateTime lastWriteTime;
        PersistanceType persistanceType;
        
        public Win32Credential()
            : this(null)
        {
        }

        public Win32Credential(string username)
            : this(username, null)
        {
        }

        public Win32Credential(string username, string password)
            : this(username, password, null)
        {
        }

        public Win32Credential(string username, string password, string target)
            : this(username, password, target, CredentialType.Generic)
        {
        }

        public Win32Credential(string username, string password, string target, CredentialType type)
        {
            Username = username;
            Password = password;
            Target = target;
            Type = type;
            PersistanceType = PersistanceType.Session;
            lastWriteTime = DateTime.MinValue;
        }


        public void Dispose()
        {
            Dispose(true);

            // Prevent GC Collection since we have already disposed of this object
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    SecurePassword.Clear();
                    SecurePassword.Dispose();
                }
            }
            disposed = true;
        }

        private void CheckNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(SR.CredentialServiceWin32CredentialDisposed);
            }
        }


        public string Username {
            get
            {
                CheckNotDisposed();
                return username;
            }
            set
            {
                CheckNotDisposed();
                username = value;
            }
        }
        public string Password
        {
            get
            {
                return SecureStringHelper.CreateString(SecurePassword);
            }
            set
            {
                CheckNotDisposed();
                SecurePassword = SecureStringHelper.CreateSecureString(string.IsNullOrEmpty(value) ? string.Empty : value);
            }
        }
        public SecureString SecurePassword
        {
            get
            {
                CheckNotDisposed();
                return null == password ? new SecureString() : password.Copy();
            }
            set
            {
                CheckNotDisposed();
                if (null != password)
                {
                    password.Clear();
                    password.Dispose();
                }
                password = null == value ? new SecureString() : value.Copy();
            }
        }
        public string Target
        {
            get
            {
                CheckNotDisposed();
                return target;
            }
            set
            {
                CheckNotDisposed();
                target = value;
            }
        }

        public string Description
        {
            get
            {
                CheckNotDisposed();
                return description;
            }
            set
            {
                CheckNotDisposed();
                description = value;
            }
        }

        public DateTime LastWriteTime
        {
            get
            {
                return LastWriteTimeUtc.ToLocalTime();
            }
        }
        public DateTime LastWriteTimeUtc 
        { 
            get
            {
                CheckNotDisposed();
                return lastWriteTime;
            }
            private set { lastWriteTime = value; }
        }

        public CredentialType Type
        {
            get
            {
                CheckNotDisposed();
                return type;
            }
            set
            {
                CheckNotDisposed();
                type = value;
            }
        }

        public PersistanceType PersistanceType
        {
            get
            {
                CheckNotDisposed();
                return persistanceType;
            }
            set
            {
                CheckNotDisposed();
                persistanceType = value;
            }
        }

        public bool Save()
        {
            CheckNotDisposed();

            byte[] passwordBytes = Encoding.Unicode.GetBytes(Password);
            if (Password.Length > (512))
            {
                throw new ArgumentOutOfRangeException(SR.CredentialsServicePasswordLengthExceeded);
            }

            NativeMethods.CREDENTIAL credential = new NativeMethods.CREDENTIAL();
            credential.TargetName = Target;
            credential.UserName = Username;
            credential.CredentialBlob = Marshal.StringToCoTaskMemUni(Password);
            credential.CredentialBlobSize = passwordBytes.Length;
            credential.Comment = Description;
            credential.Type = (int)Type;
            credential.Persist = (int) PersistanceType;

            bool result = NativeMethods.CredWrite(ref credential, 0);
            if (!result)
            {
                return false;
            }
            LastWriteTimeUtc = DateTime.UtcNow;
            return true;
        }

        public bool Delete()
        {
            CheckNotDisposed();

            if (string.IsNullOrEmpty(Target))
            {
                throw new InvalidOperationException(SR.CredentialsServiceTargetForDelete);
            }

            StringBuilder target = string.IsNullOrEmpty(Target) ? new StringBuilder() : new StringBuilder(Target);
            bool result = NativeMethods.CredDelete(target, Type, 0);
            return result;
        }

        public bool Load()
        {
            CheckNotDisposed();

            IntPtr credPointer;

            bool result = NativeMethods.CredRead(Target, Type, 0, out credPointer);
            if (!result)
            {
                return false;
            }
            using (NativeMethods.CriticalCredentialHandle credentialHandle = new NativeMethods.CriticalCredentialHandle(credPointer))
            {
                LoadInternal(credentialHandle.GetCredential());
            }
            return true;
        }

        public bool Exists()
        {
            CheckNotDisposed();

            if (string.IsNullOrEmpty(Target))
            {
                throw new InvalidOperationException(SR.CredentialsServiceTargetForLookup);
            }

            using (Win32Credential existing = new Win32Credential { Target = Target, Type = Type })
            {
                return existing.Load();
            }
        }

        internal void LoadInternal(NativeMethods.CREDENTIAL credential)
        {
            Username = credential.UserName;
            if (credential.CredentialBlobSize > 0)
            {
                Password = Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2);
            }
            Target = credential.TargetName;
            Type = (CredentialType)credential.Type;
            PersistanceType = (PersistanceType)credential.Persist;
            Description = credential.Comment;
            LastWriteTimeUtc = DateTime.FromFileTimeUtc(credential.LastWritten);
        }
    }
}