//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.TaskServices
{
    /// <summary>
    /// Task script message
    /// </summary>
    public class TaskScript
    {
        /// <summary>
        /// Status of script
        /// </summary>
        public SqlTaskStatus Status { get; set; }

        /// <summary>
        /// Script content
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Error occurred during script
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
