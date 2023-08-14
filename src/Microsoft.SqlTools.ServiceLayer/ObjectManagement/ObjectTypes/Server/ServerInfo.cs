//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#nullable disable

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// A class for storing various properties needed for Saving & Scripting a server
    /// </summary>
    public class ServerInfo : SqlObject
    {
        public string? HardwareGeneration { get; set; }
        public string Language { get; set; }
        public int MemoryInMB { get; set; }
        public string OperatingSystem { get; set; }
        public string Platform { get; set; }
        public int Processors { get; set; }
        public bool IsClustered { get; set; }
        public bool IsHadrEnabled { get; set; }
        public bool IsPolyBaseInstalled { get; set; }
        public bool? IsXTPSupported { get; set; }
        public string Product { get; set; }
        public int? ReservedStorageSizeMB { get; set; }
        public string RootDirectory { get; set; }
        public string ServerCollation { get; set; }
        public string? ServiceTier { get; set; }
        public int? StorageSpaceUsageInMB { get; set; }
        public string Version { get; set; }
        public NumericServerProperty MaxServerMemory { get; set; }
        public NumericServerProperty MinServerMemory { get; set; }
        public bool AutoProcessorAffinityMaskForAll { get; set; }
        public bool AutoProcessorAffinityIOMaskForAll { get; set; }
        public List<NumaNode> NumaNodes { get; set; }
    }

    public class NumericServerProperty
    {
        public int MaximumValue { get; set; }
        public int MinimumValue { get; set; }
        public int Value { get; set; }
    }
    public class NumaNode
    {
        public string NumaNodeId { get; set; }
        public List<ProcessorAffinity> Processors { get; set; }
    }

    public class ProcessorAffinity
    {
        public string ProcessorId { get; set; }
        public bool Affinity { get; set; }
        public bool IOAffinity { get; set; }
    }
}