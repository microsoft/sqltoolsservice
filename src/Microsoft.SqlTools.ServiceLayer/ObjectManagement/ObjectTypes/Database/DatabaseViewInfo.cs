
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#nullable disable

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class DatabaseViewInfo : SqlObjectViewInfo
    {
        public OptionsCollection LoginNames { get; set; }
        public OptionsCollection CollationNames { get; set; }
        public OptionsCollection CompatibilityLevels { get; set; }
        public OptionsCollection ContainmentTypes { get; set; }
        public OptionsCollection RecoveryModels { get; set; }

        public bool IsAzureDB { get; set; }
        public bool IsManagedInstance { get; set; }
        public bool IsSqlOnDemand { get; set; }
        public string[] AzureBackupRedundancyLevels { get; set; }
        public AzureEditionDetails[] AzureServiceLevelObjectives { get; set; }
        public string[] AzureEditions { get; set; }
        public AzureEditionDetails[] AzureMaxSizes { get; set; }
        public string[] PageVerifyOptions { get; set; }
        public string[] RestrictAccessOptions { get; set; }
        public string[] PropertiesOnOffOptions { get; set; }
        public string[] DscElevateOptions { get; set; }
        public string[] DscEnableDisableOptions { get; set; }
        public string[] FileTypesOptions { get; set; }
        public string[] OperationModeOptions { get; set; }
        public string[] StatisticsCollectionIntervalOptions { get; set; }
        public string[] QueryStoreCaptureModeOptions { get; set; }
        public string[] SizeBasedCleanupModeOptions { get; set; }
        public string[] StaleThresholdOptions { get; set; }
        public FileStreamEffectiveLevel? ServerFilestreamAccessLevel { get; set; }
        public RestoreDatabaseInfo RestoreDatabaseInfo {  get; set; }
    }

    public class AzureEditionDetails
    {
        public string EditionDisplayName { get; set; }
        public OptionsCollection EditionOptions { get; set; }
    }

    public class OptionsCollection {
        public string[] Options { get; set; }
        public int DefaultValueIndex { get; set; }
    }

    public class RestoreDatabaseInfo
    {
        public string[] SourceDatabaseNames { get; set; }
        public string[] TargetDatabaseNames { get; set; }
        public CategoryValue[] RecoveryStateOptions { get; set; }
        public string LastBackupTaken { get; set; }
    }

    public class CategoryValue
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
    }
}