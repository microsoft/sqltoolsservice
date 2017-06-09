//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts
{
    public class CancelTaskParams
    {
        /// <summary>
        /// An id to unify the task
        /// </summary>
        public string TaskId { get; set; }
    }

    public class CancelTaskRequest
    {
        public static readonly
            RequestType<CancelTaskParams, bool> Type =
                RequestType<CancelTaskParams, bool>.Create("tasks/cancel");
    }
}
