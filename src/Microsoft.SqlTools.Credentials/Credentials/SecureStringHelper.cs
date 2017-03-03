//
// Code originally from http://credentialmanagement.codeplex.com/, 
// Licensed under the Apache License 2.0 
//

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.SqlTools.Credentials
{
    internal static class SecureStringHelper
    {
        // Methods
        internal static SecureString CreateSecureString(string plainString)
        {
            SecureString str = new SecureString();
            if (!string.IsNullOrEmpty(plainString))
            {
                foreach (char c in plainString)
                {
                    str.AppendChar(c);
                }
            }
            str.MakeReadOnly();
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
