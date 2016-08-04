using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    public class ResultSetSubset
    {
        public int RowCount { get; set; }
        public object[][] Rows { get; set; }
    }
}
