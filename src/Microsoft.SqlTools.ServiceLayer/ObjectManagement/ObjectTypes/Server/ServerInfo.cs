//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.ServerConfigurations.Contracts;

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
        public int? Processors { get; set; }
        public bool? IsClustered { get; set; }
        public bool? IsHadrEnabled { get; set; }
        public bool? IsPolyBaseInstalled { get; set; }
        public bool? IsXTPSupported { get; set; }
        public string? Product { get; set; }
        public int? ReservedStorageSizeMB { get; set; }
        public string? RootDirectory { get; set; }
        public string? ServerCollation { get; set; }
        public string? ServiceTier { get; set; }
        public int? StorageSpaceUsageInGB { get; set; }
        public string? Version { get; set; }
        public ServerConfigProperty? MaxServerMemory { get; set; }
        public ServerConfigProperty? MinServerMemory { get; set; }
    }
}