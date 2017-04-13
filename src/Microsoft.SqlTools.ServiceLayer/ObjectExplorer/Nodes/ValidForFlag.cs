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
        NotDebugInstance = 0x20,
        NotContainedUser = 0x40,
        AzureV12 = 0x80,
        Sql2016 = 0x100,
        CanConnectToMaster = 0x200,
        CanViewSecurity = 0x400,
        SqlvNext = 0x800,
        All = Sql2005 | Sql2008 | Sql2012 | Sql2014 | Sql2016 | SqlvNext | Azure | AzureV12
    }
}
