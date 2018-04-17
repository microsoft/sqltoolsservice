//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Execution mode enumeration Success if execution succeeded of Failure otherwise for now.
    /// This enumeration might be refined more as there are needs for it
    /// </summary>
	public enum ExecutionMode
    {
        /// <summary>
        /// indicates that the operation failed
        /// </summary>
        Failure = 0,
        
        /// <summary>
        /// indicates that the operation succeded
        /// </summary>
        Success,

        /// <summary>
        /// indicates that the operation was canceled
        /// </summary>
        Cancel
    };

}
