//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// Table component properties
    /// </summary>
    public abstract class TableProperties<T> : ComponentPropertiesBase where T:ObjectDataModelBase
    {
        /// <summary>
        /// The column names to be displayed
        /// </summary>
        public string[] Columns { get; set; }

        /// <summary>
        /// The object type display name of the objects in this table
        /// </summary>
        public string ObjectTypeDisplayName { get; set; }

        /// <summary>
        /// All properties of the object.
        /// </summary>
        public List<DesignerDataPropertyInfo> ItemProperties { get; set; } = new List<DesignerDataPropertyInfo>();

        /// <summary>
        /// The object list.
        /// </summary>
        public List<T> Data { get; set; } = new List<T>();

        /// <summary>
        /// Add a new object into the Data property
        /// </summary>
        public void AddNew()
        {
            this.Data.Add(this.CreateNew(this.GetDefaultNewObjectName()));
        }

        protected abstract string NewObjectNamePrefix { get; }

        protected abstract T CreateNew(string name);

        /// <summary>
        /// Get the next available name for a new item
        /// </summary>
        protected string GetDefaultNewObjectName()
        {
            int i = 1;
            string newName;
            do
            {
                newName = string.Format("{0}{1}", this.NewObjectNamePrefix, i);
                i++;
            } while (this.Data != null
            && this.Data.AsEnumerable().FirstOrDefault(obj =>
            {
                return obj?.Name != null && string.Equals(obj.Name.Value, newName, StringComparison.InvariantCultureIgnoreCase);
            }) != null);
            return newName;
        }
    }
}