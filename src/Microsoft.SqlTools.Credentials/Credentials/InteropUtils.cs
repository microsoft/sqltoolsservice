//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.SqlTools.Credentials
{
    internal static class InteropUtils
    {

        /// <summary>
        /// Gets the length in bytes for a Unicode string, for use in interop where length must be defined
        /// </summary>
        public static UInt32 GetLengthInBytes(string value)
        {
            
            return Convert.ToUInt32( (value != null ? Encoding.Unicode.GetByteCount(value) : 0) );
        }

        public static string CopyToString(IntPtr ptr, int length)
        {
            if (ptr == IntPtr.Zero || length == 0)
            {
                return null;
            }
            byte[] pwdBytes = new byte[length];
            Marshal.Copy(ptr, pwdBytes, 0, (int)length);
            return Encoding.Unicode.GetString(pwdBytes, 0, (int)length);
        }

    }
}