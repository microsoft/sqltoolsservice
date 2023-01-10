//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer
{
    /// <summary>
    /// Indicates which type of server a given node type is valid for
    /// </summary>
    [Flags]
    public enum ValidForFlag
    {
        None = 0x00,
        Sql2005 = 0x01,
        Sql2008 = 0x02,
        Sql2012 = 0x04,
        Sql2014 = 0x08,
        AzureV12 = 0x10,
        Sql2016 = 0x20,
        Sql2017 = 0x40,
        SqlDw = 0x80,
        SqlOnDemand = 0x100,
        AzureSqlDWGen3 = 0x200,
        Sql2019 = 0x400,
        Sql2022 = 0x800,
        Sql2022OrHigher = Sql2022,
        Sql2017OrHigher = Sql2017 | Sql2019 | Sql2022OrHigher,
        Sql2016OrHigher = Sql2016 | Sql2017OrHigher,
        Sql2012OrHigher = Sql2012 | Sql2014 | Sql2016OrHigher,
        Sql2008OrHigher = Sql2008 | Sql2012OrHigher,
        AllOnPrem = Sql2005 | Sql2008OrHigher,
        AllAzure = AzureV12,
        All = AllOnPrem | AzureV12 | SqlDw | SqlOnDemand,
        NotSqlDw = AllOnPrem | AzureV12 | SqlOnDemand,
        NotSqlDemand = AllOnPrem | AzureV12 | SqlDw,
        NotSqlDwNotDemand = AllOnPrem | AzureV12,
    }
}
