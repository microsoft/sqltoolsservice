//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.TaskServices
{
    public class TaskRequestDetails : GeneralRequestDetails
    {
        /// <summary>
        /// The executation mode for the operation. default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }        
    }
}
