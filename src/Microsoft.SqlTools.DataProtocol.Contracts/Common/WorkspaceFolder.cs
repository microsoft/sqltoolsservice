//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.Common
{
    public class WorkspaceFolder
    {
        /// <summary>
        /// Name of the workspace folder. Defaults to <see cref="Uri"/>'s basename
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Associated URI for this workspace folder
        /// </summary>
        public string Uri { get; set; }
    }
}