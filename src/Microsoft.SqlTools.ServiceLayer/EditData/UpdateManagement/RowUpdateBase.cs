using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    public abstract class RowUpdateBase
    {
        /// <summary>
        /// Converts the row update into a SQL statement
        /// </summary>
        /// <returns></returns>
        public abstract string GetScript();

        public abstract string UpdateCell(int columnId, string newValue);
    }
}
