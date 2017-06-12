//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts
{
    /// <summary>
    /// Expand notification mapping entry 
    /// </summary>
    public class TaskCreatedNotification
    {
        public static readonly
            EventType<TaskInfo> Type =
            EventType<TaskInfo>.Create("task/newtaskcreated");
    }

    /// <summary>
    /// Expand notification mapping entry 
    /// </summary>
    public class TaskStatusChangedNotification
    {
        public static readonly
            EventType<TaskProgressInfo> Type =
            EventType<TaskProgressInfo>.Create("task/statuschanged");
    }
}
