//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    public class TaskMessage
    {
        public SqlTaskStatus Status { get; set; }

        public string Description { get; set; }
    }
}
