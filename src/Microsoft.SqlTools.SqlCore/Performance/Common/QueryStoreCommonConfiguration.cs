//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.SqlCore.Performance.Common
{
    public static class QueryStoreCommonConfiguration
    {
        static QueryStoreCommonConfiguration() => DisplayTimeKind = DateTimeKind.Local;

        public static DateTimeKind DisplayTimeKind { get; set; }
    }
}
