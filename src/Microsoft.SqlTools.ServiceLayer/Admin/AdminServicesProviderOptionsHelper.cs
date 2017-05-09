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
                    }
                }
            };
        }
    }
}
