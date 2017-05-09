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
        internal static AdminServicesProviderOptions BuildAdminServicesProviderOptions()
        {
            return new AdminServicesProviderOptions
            {
                DatabaseInfoOptions = new ServiceOption[]
                {
                    new ServiceOption
                    {
                        Name = "name",
                        DisplayName = "Name",
                        Description = "Name of the database",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = "owner",
                        DisplayName = "Owner",
                        Description = "Database owner",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = "collation",
                        DisplayName = "Collation",
                        Description = "Database collation",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = "fileGroups",
                        DisplayName = "File Groups",
                        Description = "File groups",
                        ObjectType = "FileGroupInfo",
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
                        Name = "name",
                        DisplayName = "Name",
                        Description = "Name of the file group",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                    new ServiceOption
                    {
                        Name = "databaseFiles",
                        DisplayName = "Database Files",
                        Description = "Database Files",
                        ObjectType = "DatabaseFileInfo",
                        ValueType = ServiceOption.ValueTypeObject,
                        IsRequired = true,
                        IsArray = true,
                        GroupName = "General"
                    }
                },
                DatabaseFileInfoOptions = new ServiceOption[]
                {
                    new ServiceOption
                    {
                        Name = "name",
                        DisplayName = "Name",
                        Description = "Name of the database file",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    },
                     new ServiceOption
                    {
                        Name = "physicalName",
                        DisplayName = "Physical Name",
                        Description = "Name of the database file",
                        ValueType = ServiceOption.ValueTypeString,
                        IsRequired = true,
                        GroupName = "General"
                    }
                }
            };
        }
    }
}
