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
        public string Icon { get; set; }
        public string Label { get; set; }
        public bool IsLeaf { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Name { get; set; }
        public TreeNode Parent { get; set; }
        public List<TreeNode> Children { get; set; }
        public ScriptingObject scriptingObject { get; set; }
        public bool AddParentInScriptingObject { get; set; } = false;
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
        public TreeNode(TreeNode parent, ObjectMetadata metadata)
        {
            this.Parent = parent;
            if (metadata != null)
            {
                this.Label = metadata.DisplayName;
                this.Type = metadata.Type;
                this.Name = metadata.Name;
                this.SchemaName = metadata.SchemaName;
                this.SubType = metadata.Subtype;
            }
            this.scriptingObject = new ScriptingObject()
            {
                Name = this.Name,
                Schema = this.SchemaName,
                Type = this.Type,
            };
            if(AddParentInScriptingObject)
            {
                // Find first non folder parent
                TreeNode currentParent = this.Parent;
                while(currentParent != null && currentParent.Type == "Folder")
                {
                    currentParent = currentParent.Parent;
                }
                if(currentParent != null)
                {
                    this.scriptingObject.ParentName = currentParent.Name;
                    this.scriptingObject.ParentTypeName = currentParent.Type;
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
            Icon = "Folder";
        }

    }
}