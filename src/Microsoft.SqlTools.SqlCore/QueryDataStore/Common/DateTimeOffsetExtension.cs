//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.Common
{
    public static class DateTimeOffsetExtension
    {
        public static DateTime ToDateTimeBasedOnConfiguration(this DateTimeOffset dateTimeOffset) => (QueryStoreCommonConfiguration.DisplayTimeKind == DateTimeKind.Local) ? dateTimeOffset.LocalDateTime : dateTimeOffset.UtcDateTime;
    }
}
