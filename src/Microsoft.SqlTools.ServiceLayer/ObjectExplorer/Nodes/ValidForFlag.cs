using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes
{
    /// <summary>
    /// Indicates which type of server a given node type is valid for
    /// </summary>
    [Flags]
    public enum ValidForFlag
    {
        Sql2005 = 0x01,
        Sql2008 = 0x02,
        Sql2012 = 0x04,
        Sql2014 = 0x08,
        Azure = 0x10,
        AzureV12 = 0x20,
        Sql2016 = 0x40,
        Sql2017 = 0x80,
        AllOnPrem = Sql2005 | Sql2008 | Sql2012 | Sql2014 | Sql2016 | Sql2017,
        AllAzure = Azure | AzureV12,
        All = Sql2005 | Sql2008 | Sql2012 | Sql2014 | Sql2016 | Sql2017 | Azure | AzureV12
    }
}
