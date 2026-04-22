//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.Common
{
    public static class QueryStoreCommonConfiguration
    {
        static QueryStoreCommonConfiguration() => DisplayTimeKind = DateTimeKind.Local;

        public static DateTimeKind DisplayTimeKind { get; set; }
    }
}
