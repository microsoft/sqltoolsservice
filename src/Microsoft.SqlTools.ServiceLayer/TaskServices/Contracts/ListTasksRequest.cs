//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts
{

    public class ListTasksParams
    {
        bool ListActiveTasksOnly { get; set; }
    }

    public class ListTasksResponse
    {
        public TaskInfo[] Tasks { get; set; }
    }

    public class ListTasksRequest
    {
        public static readonly
            RequestType<ListTasksParams, ListTasksResponse> Type =
                RequestType<ListTasksParams, ListTasksResponse>.Create("tasks/listtasks");
    }
}
