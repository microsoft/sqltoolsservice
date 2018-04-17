//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// what type of actions does the worker know to execute
    /// </summary>
    public enum RunType
    {
        RunNow = 0,
        RunNowAndExit,
        ScriptToFile,
        ScriptToWindow,
        ScriptToClipboard,
        ScriptToJob
    }
    
}
