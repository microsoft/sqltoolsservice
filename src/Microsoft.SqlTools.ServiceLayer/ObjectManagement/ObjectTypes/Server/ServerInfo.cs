//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// A class for storing various properties needed for Saving & Scripting a server
    /// </summary>
    public class ServerInfo : SqlObject
    {
        public string? HardwareGeneration { get; set; }
        public string? Language { get; set; }
        public int? MemoryInMB { get; set; }
        public string? OperatingSystem { get; set; }
        public string? Platform { get; set; }
        public string? Processors { get; set; }
        public bool? isClustered { get; set; }
        public bool? isHadrEnabled { get; set; }
        public bool? isPolyBaseInstalled { get; set; }
        public bool? isXtpSupported { get; set; }
        public string? Product { get; set; }
        public int? ReservedStorageSpaceInGB { get; set; }
        public string? RootDirectory { get; set; }
        public string? ServerCollation { get; set; }
        public string? ServerTier { get; set; }
        public int? StorageSpaceUsageInGB { get; set; }
        public string? Version { get; set; }
    }
}