//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Diagnostics
{
    /// <summary>
    ///  constants for Diagnostics
    /// </summary>
    internal static class DiagnosticsConstants
    {
        public static int MssqlFailedLogin = 18456;

        public static int MssqlPasswordResetCode = 18488;

        public static string MssqlExpiredPassword = "mssql/expiredPassword";

        public static string MssqlWrongPassword = "mssql/wrongPassword";
    }

}