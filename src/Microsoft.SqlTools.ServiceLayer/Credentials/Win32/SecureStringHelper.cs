//
// Code originally from http://credentialmanagement.codeplex.com/, 
// Licensed under the Apache License 2.0 
//

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.SqlTools.ServiceLayer.Credentials.Win32
{
    internal static class SecureStringHelper
    {
        // Methods
        internal static unsafe SecureString CreateSecureString(string plainString)
        {
            SecureString str;
            if (string.IsNullOrEmpty(plainString))
            {
                return new SecureString();
            }
            fixed (char* str2 = plainString)
            {
                char* chPtr = str2;
                str = new SecureString(chPtr, plainString.Length);
                str.MakeReadOnly();
            }
            return str;
        }

        internal static string CreateString(SecureString value)
        {
            IntPtr ptr = SecureStringMarshal.SecureStringToGlobalAllocUnicode(value);
            try
            {
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}
