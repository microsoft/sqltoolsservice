//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer
{
    public class IObjectExplorerSession
    {
        public TreeNode Root { get; protected set; }
    }
}