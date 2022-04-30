//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// Table component properties
    /// </summary>
    public class TableComponentProperties<T> : ComponentPropertiesBase
    {
        /// <summary>
        /// The column names to be displayed
        /// </summary>
        public List<string> Columns { get; set; } = new List<string>();

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
        /// Whether new rows can be added.
        /// </summary>
        public bool CanAddRows { get; set; } = true;

        /// <summary>
        /// Whether rows can be deleted.
        /// </summary>
        public bool CanRemoveRows { get; set; } = true;

        /// <summary>
        /// Whether rows can be moved.
        /// </summary>
        public bool CanMoveRows { get; set; } = true;

        /// <summary>
        /// Whether a confirmation should be shown when a row is about to be removed.
        /// </summary>
        public bool ShowRemoveRowConfirmation { get; set; } = false;

        /// <summary>
        /// The confirmation message to be displayed when a row is about to be removed.
        /// </summary>
        public string RemoveRowConfirmationMessage { get; set; }

        /// <summary>
        /// The label for the add new button for this table.
        /// </summary>
        public string LabelForAddNewButton { get; set; }
    }
}