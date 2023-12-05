// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer2
{
    public abstract class TreeNode
    {
        public string Icon { get; set; }
        public string Label { get; set; }
        public bool isLeaf { get; set; }
        public string Type { get; set; }

        public TreeNode()
        {
        }

        abstract public TreeNode[] GetChildren();
    }
}