
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class DatabaseViewInfo : SqlObjectViewInfo
    {
        public string[] LoginNames { get; set; }
        public string[] CollationNames { get; set; }
        public string[] CompatibilityLevels { get; set; }
        public string[] ContainmentTypes { get; set; }
        public string[] RecoveryModels { get; set; }
        public DatabaseFile[] Files { get; set; }

        public bool IsAzureDB { get; set; }
        public string[] AzureBackupRedundancyLevels { get; set; }
        public AzureEditionDetails[] AzureServiceLevelObjectives { get; set; }
        public string[] AzureEditions { get; set; }
        public AzureEditionDetails[] AzureMaxSizes { get; set; }
        public string[] PageVerifyOptions { get; set; }
        public string[] RestrictAccessOptions { get; set; }
    }

    public class AzureEditionDetails
    {
        public string EditionDisplayName { get; set; }
        public string[] Details { get; set; }
    }

    public class DatabaseFile
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }
        public string FileGroup { get; set; }
    }
}