//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    public enum SqlTaskStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Succeeded = 2,
        SucceededWithWarning = 3,
        Failed = 4,
        Canceled = 5,
        CancelRequested = 6
    }
}
