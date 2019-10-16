using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        AllOnPrem = Sql2005 | Sql2008 | Sql2012 | Sql2014 | Sql2016 | Sql2017,
        AllAzure = AzureV12,
        All = Sql2005 | Sql2008 | Sql2012 | Sql2014 | Sql2016 | Sql2017 | AzureV12 | SqlDw | SqlOnDemand,
        NotSqlDw = Sql2005 | Sql2008 | Sql2012 | Sql2014 | Sql2016 | Sql2017 | AzureV12 | SqlOnDemand,
        NotSqlDemand = Sql2005 | Sql2008 | Sql2012 | Sql2014 | Sql2016 | Sql2017 | AzureV12 | SqlDw,
        NotSqlDwNotDemand = Sql2005 | Sql2008 | Sql2012 | Sql2014 | Sql2016 | Sql2017 | AzureV12,
    }
}
