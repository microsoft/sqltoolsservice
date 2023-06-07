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
        /// Gets the length in bytes for an encoded string, for use in interop where length must be defined
        /// </summary>
        /// <param name="value">String value</param>
        /// <param name="encoding">Encoding of string provided.</param>
        public static UInt32 GetLengthInBytes(string value, Encoding encoding)
        {
            if (encoding != Encoding.Unicode && encoding != Encoding.UTF8)
            {
                throw new ArgumentException($"Encoding {encoding} not supported.");
            }
            return Convert.ToUInt32((value != null
                    ? (encoding == Encoding.UTF8
                        ? Encoding.UTF8.GetByteCount(value)
                        : Encoding.Unicode.GetByteCount(value))
                    : 0));
        }

        /// <summary>
        /// Copies data of length <paramref name="length"/> from <paramref name="ptr"/>
        /// pointer to a string of provided encoding.
        /// </summary>
        /// <param name="ptr">Pointer to data</param>
        /// <param name="length">Length of data to be copied.</param>
        /// <param name="encoding">Character encoding to be used to get string.</param>
        /// <returns></returns>
        public static string? CopyToString(IntPtr ptr, int length, Encoding encoding)
        {
            if (ptr == IntPtr.Zero || length == 0)
            {
                return null;
            }
            if (encoding != Encoding.Unicode && encoding != Encoding.UTF8)
            {
                throw new ArgumentException($"Encoding {encoding} not supported.");
            }
            byte[] pwdBytes = new byte[length];
            Marshal.Copy(ptr, pwdBytes, 0, (int)length);
            return (encoding == Encoding.UTF8)
                ? Encoding.UTF8.GetString(pwdBytes, 0, (int)length)
                : Encoding.Unicode.GetString(pwdBytes, 0, (int)length);
        }

    }
}