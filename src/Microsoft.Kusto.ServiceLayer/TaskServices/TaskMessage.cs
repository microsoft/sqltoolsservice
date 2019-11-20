//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.TaskServices
{
    public class TaskMessage
    {
        public SqlTaskStatus Status { get; set; }

        public string Description { get; set; }
    }
}
