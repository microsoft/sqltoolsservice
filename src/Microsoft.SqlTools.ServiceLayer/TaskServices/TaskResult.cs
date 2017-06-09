//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    public class TaskResult
    {
        public SqlTaskStatus TaskStatus { get; set; }

        public string ErrorMessage { get; set; }
    }
}
