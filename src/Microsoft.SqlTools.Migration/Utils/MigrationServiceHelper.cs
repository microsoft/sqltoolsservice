//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;

namespace Microsoft.SqlTools.Migration.Utils
{
    internal static class MigrationServiceHelper
    {
        public static string FormatTimeSpan(TimeSpan ts)
        {
            return String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        }
    }
}
