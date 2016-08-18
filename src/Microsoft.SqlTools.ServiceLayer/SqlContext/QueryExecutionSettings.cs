using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    public class QueryExecutionSettings
    {
        private const string DefaultBatchSeparator = "GO";

        private string batchSeparator;

        public string BatchSeparator
        {
            get { return batchSeparator ?? DefaultBatchSeparator; }
            set { batchSeparator = value; }
        }

        /// <summary>
        /// Update the current settings with the new settings
        /// </summary>
        /// <param name="newSettings">The new settings</param>
        public void Update(QueryExecutionSettings newSettings)
        {
            BatchSeparator = newSettings.BatchSeparator;
        }
    }
}
