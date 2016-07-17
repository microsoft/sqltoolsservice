//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.EditorServices.Session
{
    public class SqlToolsContext
    {
        /// <summary>
        /// Gets the PowerShell version of the current runspace.
        /// </summary>
        public Version SqlToolsVersion
        {
            get; private set;
        }

        public SqlToolsContext(HostDetails hostDetails, ProfilePaths profilePaths)
        {
            
        }
    }
}
