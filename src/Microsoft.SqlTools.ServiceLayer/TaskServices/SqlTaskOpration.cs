using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    public class SqlTaskOpration
    {
        /// <summary>
        /// The function to run 
        /// </summary>
        public Func<SqlTask, Task<TaskResult>> TaskToRun
        {
            get;
            set;
        }

        /// <summary>
        /// The function to cancel the operation 
        /// </summary>
        public Func<SqlTask, Task<TaskResult>> TaskToCancel
        {
            get;
            set;
        }
    }
}
