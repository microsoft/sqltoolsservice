//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.Common
{
    public static class DateTimeOffsetExtension
    {
        public static DateTime ToDateTimeBasedOnConfiguration(this DateTimeOffset dateTimeOffset) => (QueryStoreCommonConfiguration.DisplayTimeKind == DateTimeKind.Local) ? dateTimeOffset.LocalDateTime : dateTimeOffset.UtcDateTime;
    }
}
