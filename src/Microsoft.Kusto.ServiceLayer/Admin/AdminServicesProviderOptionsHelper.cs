//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Helper class for providing metadata about admin services
    /// </summary>         
    public class AdminServicesProviderOptionsHelper
    {
        internal const string Name = "name";
        internal const string Owner = "owner";
        internal const string Collation = "collation";
        internal const string FileGroups = "fileGroups";
        internal const string DatabaseFiles = "databaseFiles";
        internal const string PhysicalName = "physicalName";
        internal const string RecursiveTriggers = "recursiveTriggers";
        internal const string Trustworthy = "trustworthy";
        internal const string AnsiNullDefault = "ansiNullDefault";
        internal const string AnsiNulls = "ansiNulls";
        internal const string AnsiPadding = "ansiNulls";
        internal const string AnsiWarnings = "ansiNulls";
        internal const string IsFilestreamEnabled = "isFilestreamEnabled";
        internal const string IsReadCommittedSnapshotOn = "isReadCommittedSnapshotOn";
        internal const string IsReadOnly = "isReadOnly";
        internal const string IsSystemDB = "isSystemDB";
        internal const string MaxDop = "maxDop";
        internal const string DatabaseContainmentType = "databaseContainmentType";
        internal const string DatabaseState = "databaseState";
        internal const string RecoveryModel = "recoveryModel";
        internal const string CompatibilityLevel = "compatibilityLevel";
        internal const string LastBackupDate = "lastBackupDate";
        internal const string LastLogBackupDate = "lastLogBackupDate";
        internal const string FileGroupType = "fileGroupType";
        internal const string IsDefault = "isDefault";
        internal const string IsFileStream = "isFileStream";
        internal const string IsMemoryOptimized = "isMemoryOptimized";
        internal const string Autogrowth = "autogrowth";
        internal const string DatabaseFileType = "databaseFileType";
        internal const string Folder = "folder";
        internal const string Size = "size";
        internal const string FileGroup = "fileGroup";
        internal const string InitialSize = "initialSize";
        internal const string IsPrimaryFile = "isPrimaryFile";
        internal const string AzureEdition = "azureEdition";
        internal const string ServiceLevelObjective = "serviceLevelObjective";


        internal static AdminServicesProviderOptions BuildAdminServicesProviderOptions()
        {
            return new AdminServicesProviderOptions
            {
                DatabaseInfoOptions = new ServiceOption[]
                {
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.Name,
                        DisplayName = "Name",
                        Description = "Name of the database",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.Owner,
                        DisplayName = "Owner",
                        Description = "Database owner",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.Collation,
                        DisplayName = "Collation",
                        Description = "Database collation",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.RecursiveTriggers,
                        DisplayName = "Recursive Triggers",
                        Description = "Recursive triggers",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.Trustworthy,
                        DisplayName = "Trustworthy",
                        Description = "Trustworthy",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.AnsiNullDefault,
                        DisplayName = "AnsiNullDefault",
                        Description = "Ansi null default",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.AnsiNulls,
                        DisplayName = "AnsiNulls",
                        Description = "AnsiNulls",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.AnsiPadding,
                        DisplayName = "AnsiPadding",
                        Description = "Ansi padding",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.AnsiWarnings,
                        DisplayName = "AnsiWarnings",
                        Description = "Ansi warnings",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.IsFilestreamEnabled,
                        DisplayName = "IsFilestreamEnabled",
                        Description = "Is filestream enabled",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.IsReadCommittedSnapshotOn,
                        DisplayName = "IsReadCommittedSnapshotOn",
                        Description = "Is read committed snapshot on",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.IsReadOnly,
                        DisplayName = "IsReadOnly",
                        Description = "Is read only",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.IsSystemDB,
                        DisplayName = "IsSystemDB",
                        Description = "Is system database",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.MaxDop,
                        DisplayName = "MaxDop",
                        Description = "Max degree of parallelism",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.DatabaseContainmentType, 
                        DisplayName = "DatabaseContainmentType",
                        Description = "Database containment type",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.DatabaseState,
                        DisplayName = "DatabaseState",
                        Description = "Database state",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.RecoveryModel,
                        DisplayName = "RecoveryModel",
                        Description = "Recovery model",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.LastBackupDate,
                        DisplayName = "LastBackupDate",
                        Description = "Last backup date",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.LastLogBackupDate,
                        DisplayName = "LastLogBackupDate",
                        Description = "Last log backup date",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.CompatibilityLevel,
                        DisplayName = "CompatibilityLevel",
                        Description = "Compatibility level",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = false,
                        GroupName = "Other"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.FileGroups,
                        DisplayName = "File Groups",
                        Description = "File groups",
                        ObjectType = "FileGroupInfo",
                        ValueType = ServiceOption.ValueTypeObject,
                        IsRequired = true,
                        IsArray = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.DatabaseFiles,
                        DisplayName = "Database Files",
                        Description = "Database Files",
                        ObjectType = "DatabaseFileInfo",
                        ValueType = ServiceOption.ValueTypeObject,
                        IsRequired = true,
                        IsArray = true,
                        GroupName = "General"
                    }
                },
                FileGroupInfoOptions = new ServiceOption[]
                {
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.Name,
                        DisplayName = "Name",
                        Description = "Name of the file group",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.FileGroupType,
                        DisplayName = "FileGroupType",
                        Description = "File group type",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.IsDefault,
                        DisplayName = "IsDefault",
                        Description = "Is default",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.IsFileStream,
                        DisplayName = "IsFileStream",
                        Description = "Is file stream",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.IsMemoryOptimized,
                        DisplayName = "IsMemoryOptimized",
                        Description = "Is memory optimized",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.IsReadOnly,
                        DisplayName = "IsReadOnly",
                        Description = "Is read-only",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    }              
                },


                DatabaseFileInfoOptions = new ServiceOption[]
                {
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.Name,
                        DisplayName = "Name",
                        Description = "Name of the database file",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.PhysicalName,
                        DisplayName = "Physical Name",
                        Description = "Physical name of the database file",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },

                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.Autogrowth,
                        DisplayName = "Autogrowth",
                        Description = "Autogrowth",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.DatabaseFileType,
                        DisplayName = "DatabaseFileType",
                        Description = "Database file type",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.Folder,
                        DisplayName = "Folder",
                        Description = "Folder",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.Size,
                        DisplayName = "Size",
                        Description = "Size",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.FileGroup,
                        DisplayName = "FileGroup",
                        Description = "File group",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.InitialSize,
                        DisplayName = "InitialSize",
                        Description = "Initial size",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = AdminServicesProviderOptionsHelper.IsPrimaryFile,
                        DisplayName = "IsPrimaryFile",
                        Description = "Is primary file",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                }
            };
        }
    }
}
