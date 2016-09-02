//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.InteropServices;

namespace Microsoft.SqlTools.ServiceLayer.Credentials.Linux
{
    internal static partial class Interop
    {
        internal static partial class Sys
        {
            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ChMod", SetLastError = true)]
            internal static extern int ChMod(string path, int mode);
        }


        private static partial class Libraries
        {
            internal const string SystemNative = "System.Native";
        }
    }
}
