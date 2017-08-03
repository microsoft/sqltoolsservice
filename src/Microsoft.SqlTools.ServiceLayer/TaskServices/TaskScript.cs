//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    public class TaskScript
    {
        /// <summary>
        /// 
        /// </summary>
        public SqlTaskStatus Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
