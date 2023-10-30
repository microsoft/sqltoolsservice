//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;

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
        public DatabaseFile[] Files { get; set; }
        public FileGroupSummary[]? Filegroups { get; set; }
        public QueryStoreOptions? QueryStoreOptions { get; set; }
        public BackupEncryptor[]? BackupEncryptors { get; set; }
        public RestoreOptions restoreOptions { get; set; }
    }

    public class DatabaseScopedConfigurationsInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ValueForPrimary { get; set; }
        public string ValueForSecondary { get; set; }
    }

    public class DatabaseFile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }
        public string FileGroup { get; set; }
        public string FileNameWithExtension { get; set; }
        public double SizeInMb { get; set; }
        public bool IsAutoGrowthEnabled { get; set; }
        public double AutoFileGrowth { get; set; }
        public FileGrowthType AutoFileGrowthType { get; set; }
        public double MaxSizeLimitInMb { get; set; }
    }

    public class FileGroupSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public FileGroupType Type { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsDefault { get; set; }
        public bool AutogrowAllFiles { get; set; }
    }

    public class QueryStoreOptions
    {
        public string ActualMode { get; set; }
        public long DataFlushIntervalInMinutes { get; set; }
        public string StatisticsCollectionInterval { get; set; }
        public long MaxPlansPerQuery { get; set; }
        public long MaxSizeInMB { get; set; }
        public string QueryStoreCaptureMode { get; set; }
        public string SizeBasedCleanupMode { get; set; }
        public long StaleQueryThresholdInDays { get; set; }
        public string? WaitStatisticsCaptureMode { get; set; }
        public QueryStoreCapturePolicyOptions? CapturePolicyOptions { get; set; }
        public long CurrentStorageSizeInMB { get; set; }
    }

    public class QueryStoreCapturePolicyOptions
    {
        public int ExecutionCount { get; set; }
        public string StaleThreshold { get; set; }
        public long TotalCompileCPUTimeInMS { get; set; }
        public long TotalExecutionCPUTimeInMS { get; set; }
    }

    public class RestoreOptions
    {
        public bool KeepReplication{ get; set; }
        public bool ReplaceDatabase { get; set; }
        public bool SetRestrictedUser {get; set; }
        public string? RecoveryState { get; set; }
        public bool BackupTailLog { get; set; }
        public string? TailLogBackupFile { get; set; }
        public bool TailLogWithNoRecovery { get; set; }
        public bool CloseExistingConnections { get; set; }
        public bool RelocateDbFiles{ get; set; }
        public string? DataFileFolder { get; set; }
        public string? LogFileFolder { get; set; }
        public string? StandbyFile { get; set; }
    }   
}