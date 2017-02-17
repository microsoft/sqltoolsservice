using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    public class EditColumnWrapper
    {
        public DbColumnWrapper DbColumn { get; set; }

        public string EscapedName { get; set; }

        public bool IsKey { get; set; }

        public bool IsTrustworthyForUniqueness { get; set; }

        public int Ordinal { get; set; }
    }
}
