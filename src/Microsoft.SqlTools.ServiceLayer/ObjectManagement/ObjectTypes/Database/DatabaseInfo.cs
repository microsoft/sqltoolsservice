//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// A class for storing various properties needed for Saving & Scripting a database
    /// </summary>
    public class DatabaseInfo : SqlObject
    {
        public string? Owner { get; set; }
        public string? CollationName { get; set; }
        public string? RecoveryModel { get; set; }
        public string? CompatibilityLevel { get; set; }
        public string? ContainmentType { get; set; }
        public string? DateCreated { get; set; }
        public string? LastDatabaseBackup { get; set; }
        public string? LastDatabaseLogBackup { get; set; }
        public double? MemoryAllocatedToMemoryOptimizedObjectsInMb { get; set; }
        public double? MemoryUsedByMemoryOptimizedObjectsInMb { get; set; }
        public int? NumberOfUsers { get; set; }
        public double? SizeInMb { get; set; }
        public double? SpaceAvailableInMb { get; set; }
        public string? Status { get; set; }
        public string? AzureBackupRedundancyLevel { get; set; }
        public string? AzureServiceLevelObjective { get; set; }
        public string? AzureEdition { get; set; }
        public string? AzureMaxSize { get; set; }
        public bool AutoCreateIncrementalStatistics { get; set; }
        public bool AutoCreateStatistics { get; set; }
        public bool AutoShrink { get; set; }
        public bool AutoUpdateStatistics { get; set; }
        public bool AutoUpdateStatisticsAsynchronously { get; set; }
        public bool? IsLedgerDatabase { get; set; }
        public string? PageVerify { get; set; }
        public int? TargetRecoveryTimeInSec { get; set; }
        public bool? DatabaseReadOnly { get; set; }
        public bool EncryptionEnabled { get; set; }
        public string? RestrictAccess { get; set; }
        public DatabaseScopedConfigurationsInfo[]? DatabaseScopedConfigurations { get; set; }
        public bool? IsFilesTabSupported { get; set; }
        public DatabaseFile[] Files { get; set; } = null!;
        public FileGroupSummary[]? Filegroups { get; set; }
        public QueryStoreOptions? QueryStoreOptions { get; set; }
        public BackupEncryptor[]? BackupEncryptors { get; set; }
        public RestorePlanResponse restorePlanResponse { get; set; } = null!;
    }

    public class DatabaseScopedConfigurationsInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ValueForPrimary { get; set; } = null!;
        public string ValueForSecondary { get; set; } = null!;
    }

    public class DatabaseFile
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string FileGroup { get; set; } = null!;
        public string FileNameWithExtension { get; set; } = null!;
        public double SizeInMb { get; set; }
        public bool IsAutoGrowthEnabled { get; set; }
        public double AutoFileGrowth { get; set; }
        public FileGrowthType AutoFileGrowthType { get; set; }
        public double MaxSizeLimitInMb { get; set; }
    }

    public class FileGroupSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public FileGroupType Type { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsDefault { get; set; }
        public bool AutogrowAllFiles { get; set; }
    }

    public class QueryStoreOptions
    {
        public string ActualMode { get; set; } = null!;
        public long DataFlushIntervalInMinutes { get; set; }
        public string StatisticsCollectionInterval { get; set; } = null!;
        public long MaxPlansPerQuery { get; set; }
        public long MaxSizeInMB { get; set; }
        public string QueryStoreCaptureMode { get; set; } = null!;
        public string SizeBasedCleanupMode { get; set; } = null!;
        public long StaleQueryThresholdInDays { get; set; }
        public string? WaitStatisticsCaptureMode { get; set; }
        public QueryStoreCapturePolicyOptions? CapturePolicyOptions { get; set; }
        public long CurrentStorageSizeInMB { get; set; }
    }

    public class QueryStoreCapturePolicyOptions
    {
        public int ExecutionCount { get; set; }
        public string StaleThreshold { get; set; } = null!;
        public long TotalCompileCPUTimeInMS { get; set; }
        public long TotalExecutionCPUTimeInMS { get; set; }
    }
}