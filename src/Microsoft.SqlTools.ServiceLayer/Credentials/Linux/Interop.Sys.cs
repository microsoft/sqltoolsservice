//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;

namespace Microsoft.SqlTools.ServiceLayer.Credentials
{
    internal static partial class Interop
    {
        internal static partial class Sys
        {
            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ChMod", SetLastError = true)]
            internal static extern int ChMod(string path, int mode);
            
            internal struct Passwd
            {
                internal IntPtr Name;           // char*
                internal IntPtr Password;       // char*
                internal uint  UserId;
                internal uint  GroupId;
                internal IntPtr UserInfo;       // char*
                internal IntPtr HomeDirectory;  // char*
                internal IntPtr Shell;          // char*
            };

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPwUidR", SetLastError = false)]
            internal static extern int GetPwUidR(uint uid, out Passwd pwd, IntPtr buf, int bufLen);

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetEUid")]
            internal static extern uint GetEUid();

            private static partial class Libraries
            {
                internal const string SystemNative = "System.Native";
            }
        }
        
    }
}
