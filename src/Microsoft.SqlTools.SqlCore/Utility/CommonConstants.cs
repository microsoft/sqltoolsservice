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
        public const string QueryStoreOperationMode_Off = "OFF";
        public const string QueryStoreOperationMode_ReadOnly = "Read Only";
        public const string QueryStoreOperationMode_ReadWrite = "Read Write";
    }
}
