using System.Collections.Generic;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public class DiagnosticsInfo
    {
        /// <summary>
        /// Gets or sets the options
        /// </summary>
        public Dictionary<string, object> Options { get; set; }

        public DiagnosticsInfo()
        {
            Options = new Dictionary<string, object>();
        }
    }
}