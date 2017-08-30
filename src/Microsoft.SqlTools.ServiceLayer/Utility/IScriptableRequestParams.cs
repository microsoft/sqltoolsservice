//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlTools.ServiceLayer.TaskServices;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public interface IScriptableRequestParams : IRequestParams
    {
        /// <summary>
        /// The executation mode for the operation. default is execution
        /// </summary>
        TaskExecutionMode TaskExecutionMode { get; set; }
    }
}
