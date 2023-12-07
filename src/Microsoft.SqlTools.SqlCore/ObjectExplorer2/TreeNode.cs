//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer2
{
    public abstract class TreeNode
    {
        public string SchemaName { get; set; }
        public string Label { get; set; }
        public bool IsLeaf { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Name { get; set; }
        public TreeNode Parent { get; set; }
        public List<TreeNode> Children { get; set; }
        public ScriptingObject ScriptingObject { get; set; }
        public string Path
        {
            get
            {
                if (Parent == null)
                {
                    return "/";
                }
                else
                {
                    return Parent.Path + Name + "/";
                }
            }
        }
        public TreeNode(TreeNode parent, ObjectMetadata metadata, bool addParentInScriptingObject = false)
        {
            this.Parent = parent;
            if (metadata != null)
            {
                this.Label = metadata.DisplayName;
                this.Name = metadata.Name;
                this.SchemaName = metadata.Schema;
                this.SubType = metadata.SubType;
            }
            this.ScriptingObject = new ScriptingObject()
            {
                Name = this.Name,
                Schema = this.SchemaName,
            };
            if(addParentInScriptingObject)
            {
                // Find first non folder parent
                TreeNode nodFolderParent = this.Parent;
                while( nodFolderParent as FolderNode != null)
                {
                    nodFolderParent = nodFolderParent.Parent;
                }

                if(nodFolderParent != null)
                {
                    this.ScriptingObject.ParentName = nodFolderParent.ScriptingObject.Name;
                    this.ScriptingObject.ParentTypeName = nodFolderParent.ScriptingObject.Type;
                }
            }
        }
        abstract public void LoadChildren(ObjectMetadata[] metadata);
    }

    public abstract class FolderNode : TreeNode
    {
        public FolderNode(TreeNode parent) : base(parent, null)
        {
            IsLeaf = false;
            Type = "Folder";
        }

    }
}