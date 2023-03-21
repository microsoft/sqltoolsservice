//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;
using static Microsoft.SqlTools.Credentials.Interop.Security;

namespace Microsoft.SqlTools.Credentials
{
    internal partial class Interop
    {
        internal class SecurityOld
        {
            /// <summary>
            /// Find a generic password based on the attributes passed (using Unicode encoding)
            /// </summary>
            /// <param name="keyChainRef">
            /// A reference to an array of keychains to search, a single keychain, or NULL to search the user's default keychain search list.
            /// </param>
            /// <param name="serviceNameLength">The length of the buffer pointed to by serviceName.</param>
            /// <param name="serviceName">A pointer to a string containing the service name.</param>
            /// <param name="accountNameLength">The length of the buffer pointed to by accountName.</param>
            /// <param name="accountName">A pointer to a string containing the account name.</param>
            /// <param name="passwordLength">On return, the length of the buffer pointed to by passwordData.</param>
            /// <param name="password">
            /// On return, a pointer to a data buffer containing the password.
            /// Your application must call SecKeychainItemFreeContent(NULL, passwordData)
            /// to release this data buffer when it is no longer needed.Pass NULL if you are not interested in retrieving the password data at
            /// this time, but simply want to find the item reference.
            /// </param>
            /// <param name="itemRef">On return, a reference to the keychain item which was found.</param>
            /// <returns>A result code that should be in <see cref="OSStatus"/></returns>
            /// <remarks>
            /// The SecKeychainFindGenericPassword function finds the first generic password item which matches the attributes you provide.
            /// Most attributes are optional; you should pass only as many as you need to narrow the search sufficiently for your application's intended use.
            /// SecKeychainFindGenericPassword optionally returns a reference to the found item.
            /// </remarks>
            /// 
            /// ***********************************************************************************************
            /// This method is marked OBSOLETE as it used 'Unicode' Charset encoding to save credentials.
            /// It has been replaced with respective API in the 'Security' class using 'Auto' Charset encoding.
            /// ***********************************************************************************************
            [Obsolete]
            [DllImport(Libraries.SecurityLibrary, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern OSStatus SecKeychainFindGenericPassword(IntPtr keyChainRef, UInt32 serviceNameLength, string serviceName,
                UInt32 accountNameLength, string accountName, out UInt32 passwordLength, out IntPtr password, out IntPtr itemRef);
        }
    }
}

