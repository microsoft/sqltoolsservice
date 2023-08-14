
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class DatabaseViewInfo : SqlObjectViewInfo
    {
        public OptionsCollection LoginNames { get; set; }
        public OptionsCollection CollationNames { get; set; }
        public OptionsCollection CompatibilityLevels { get; set; }
        public OptionsCollection ContainmentTypes { get; set; }
        public OptionsCollection RecoveryModels { get; set; }
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
        public OptionsCollection EditionOptions { get; set; }
    }

    public class DatabaseFile
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }
        public string FileGroup { get; set; }
    }

    public class OptionsCollection {
        public string[] Options { get; set; }
        public int DefaultValueIndex { get; set; }
    }
}