using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts
{
    public enum TaskState
    {
        NotStarted = 0,
        Running = 1,
        Complete = 2
    }

    public class TaskInfo
    {
        public int TaskId { get; set;  }

        public TaskState State { get;  set; }
    }
}
