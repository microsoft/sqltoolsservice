﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable


using System;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DisasterRecovery
{
    [Flags]
    public enum TaskExecutionModeFlag
    {
        None = 0x00,

        /// <summary>
        /// Execute task
        /// </summary>
        Execute = 0x01,

        /// <summary>
        /// Script task
        /// </summary>
        Script = 0x02,

        /// <summary>
        /// Execute and script task
        /// Needed for tasks that will show the script when execution completes
        /// </summary>
        ExecuteAndScript = Execute | Script
    }
}
