//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.SqlCore.Utility
{
    /// <summary>
    /// Common Constant values used across multiple services
    /// </summary>
    public static class CommonConstants
    {
        public const string MasterDatabaseName = "master";
        public const string MsdbDatabaseName = "msdb";
        public const string ModelDatabaseName = "model";
        public const string TempDbDatabaseName = "tempdb";

        public const string DefaultBatchSeperator = "GO";

        // Database scoped configurations property values
        public const string DatabaseScopedConfigurations_Value_On = "ON";
        public const string DatabaseScopedConfigurations_Value_Off = "OFF";
        public const string DatabaseScopedConfigurations_Value_Primary = "PRIMARY";
        public const string DatabaseScopedConfigurations_Value_When_supported = "WHEN_SUPPORTED";
        public const string DatabaseScopedConfigurations_Value_Fail_Unsupported = "FAIL_UNSUPPORTED";
        public const string DatabaseScopedConfigurations_Value_Enabled = "ENABLED";
        public const string DatabaseScopedConfigurations_Value_Disabled = "DISABLED";
        public const string QueryStoreOperationMode_Off = "Off";
        public const string QueryStoreOperationMode_ReadOnly = "Read Only";
        public const string QueryStoreOperationMode_ReadWrite = "Read Write";
        public const string QueryStoreCaptureMode_All = "All";
        public const string QueryStoreCaptureMode_Auto = "Auto";
        public const string QueryStoreCaptureMode_None = "None";
        public const string QueryStoreCaptureMode_Custom = "Custom";
        public const string QueryStoreSizeBasedCleanupMode_Off = "Off";
        public const string QueryStoreSizeBasedCleanupMode_Auto = "Auto";

        // Need to move these below const to LOC
        public const string StatisticsCollectionInterval_OneMinute = "1 Minute";
        public const string StatisticsCollectionInterval_FiveMinutes = "5 Minutes";
        public const string StatisticsCollectionInterval_TenMinutes = "10 Minutes";
        public const string StatisticsCollectionInterval_FifteenMinutes = "15 Minutes";
        public const string StatisticsCollectionInterval_ThirtyMinutes = "30 Minutes";
        public const string StatisticsCollectionInterval_OneHour = "1 Hour";
        public const string StatisticsCollectionInterval_OneDay = "1 Day";
        public const string QueryStore_stale_threshold_OneHour = "1 Hour";
        public const string QueryStore_stale_threshold_FourHours = "4 Hours";
        public const string QueryStore_stale_threshold_EightHours = "8 Hours";
        public const string QueryStore_stale_threshold_TwelveHours = "12 Hours";
        public const string QueryStore_stale_threshold_OneDay = "1 Day";
        public const string QueryStore_stale_threshold_ThreeDays = "3 Days";
        public const string QueryStore_stale_threshold_SevenDays = "7 Days";
    }
}
