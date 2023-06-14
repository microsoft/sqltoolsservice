//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
    }
}