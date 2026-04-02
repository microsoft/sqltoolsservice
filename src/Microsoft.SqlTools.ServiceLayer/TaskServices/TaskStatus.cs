//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    public enum SqlTaskStatus
    {
        NotStarted,
        InProgress,
        Succeeded,
        SucceededWithWarning,
        Failed,
        Canceled,
        /// <summary>
        /// Cancellation has been requested but the operation has not yet stopped.
        /// This allows the UI to show "cancellation in progress" feedback.
        /// </summary>
        CancelRequested
    }
}
