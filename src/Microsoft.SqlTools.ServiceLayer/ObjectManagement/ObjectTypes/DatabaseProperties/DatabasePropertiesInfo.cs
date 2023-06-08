//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// A class for storing various properties needed for Saving & Scripting database properties
    /// </summary>
    public class DatabasePropertiesInfo : SqlObject
    {
        public string? CollationName { get; set; }
        public string? DateCreated { get; set; }
        public string? LastDatabaseBackup { get; set; }
        public string? LastDatabaseLogBackup { get; set; }
        public string? MemoryAllocatedToMemoryOptimizedObjectsInMb { get; set; }
        public string? MemoryUsedByMemoryOptimizedObjectsInMb { get; set; }
        public string? NumberOfUsers { get; set; }
        public string? Owner { get; set; }
        public string? SizeInMb { get; set; }
        public string? SpaceAvailableInMb { get; set; }
        public string? Status { get; set; }
    }
}