using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    public interface IEditColumnWrapper
    {
        DbColumnWrapper DbColumn { get; }

        string EscapedName { get; }

        bool IsKey { get; }
        
        bool IsTrustworthyForUniqueness { get; }

        int Ordinal { get; }
    }
}
