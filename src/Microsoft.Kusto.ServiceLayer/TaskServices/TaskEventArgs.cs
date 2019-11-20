//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.TaskServices
{
    public sealed class TaskEventArgs<T> : EventArgs
    {
        readonly T taskData;

        public TaskEventArgs(T taskData, SqlTask sqlTask)
        {
            Validate.IsNotNull(nameof(taskData), taskData);

            this.taskData = taskData;
            SqlTask = sqlTask;
        }


        public TaskEventArgs(SqlTask sqlTask)
        {
            taskData = (T)Convert.ChangeType(sqlTask, typeof(T));
            SqlTask = sqlTask;
        }

        public T TaskData
        {
            get
            {
                return taskData;
            }
        }

        public SqlTask SqlTask { get; set; }
    }
}
