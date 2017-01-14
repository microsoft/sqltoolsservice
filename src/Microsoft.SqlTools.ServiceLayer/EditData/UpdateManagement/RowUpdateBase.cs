using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    public abstract class RowUpdateBase
    {

        /// <summary>
        /// The ID of the row, relative to the query's internal row list. Can be omitted if row
        /// update is to a row without an ID.
        /// </summary>
        public long? RowId { get; set; }

        /// <summary>
        /// Converts the row update into a SQL statement
        /// </summary>
        /// <returns></returns>
        public abstract string GetScript();
    }
}
