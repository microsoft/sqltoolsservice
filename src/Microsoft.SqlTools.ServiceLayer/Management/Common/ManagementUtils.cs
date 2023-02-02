//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.TaskServices;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
    /// <summary>
    /// Utility functions for working with Management classes
    /// </summary>
    public static class ManagementUtils
    {
        public static RunType asRunType(TaskExecutionMode taskExecutionMode)
        {
            if (taskExecutionMode == TaskExecutionMode.Script)
            {
                return RunType.ScriptToWindow;
            }
            else
            {
                return RunType.RunNow;
            }
        }
    }
}
