//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.Kusto.ServiceLayer.TaskServices
{
    public enum SqlTaskStatus
    {
        NotStarted,
        InProgress,
        Succeeded,
        SucceededWithWarning,
        Failed,
        Canceled
    }
}
