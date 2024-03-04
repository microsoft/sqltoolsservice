//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Runtime.Serialization;
namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// Table component properties
    /// </summary>
    [DataContract]
    [KnownType(typeof(TableComponentProperties<IndexedColumnSpecification>))]
    [KnownType(typeof(TableComponentProperties<TableColumnViewModel>))]
    [KnownType(typeof(TableComponentProperties<ForeignKeyViewModel>))]
    [KnownType(typeof(TableComponentProperties<CheckConstraintViewModel>))]
    [KnownType(typeof(TableComponentProperties<EdgeConstraintViewModel>))]
    [KnownType(typeof(TableComponentProperties<IndexViewModel>))]
    [KnownType(typeof(TableComponentProperties<ColumnStoreIndexViewModel>))]
    public class TableComponentProperties<T> : ComponentPropertiesBase
    {
        /// <summary>
        /// The column names to be displayed
        /// </summary>
        [DataMember(Name = "columns")]
        public List<string> Columns { get; set; } = new List<string>();

        /// <summary>
        /// The object type display name of the objects in this table
        /// </summary>
        [DataMember(Name = "objectTypeDisplayName")]
        public string ObjectTypeDisplayName { get; set; }

        /// <summary>
        /// All properties of the object.
        /// </summary>
        [DataMember(Name = "itemProperties")]
        public List<DesignerDataPropertyInfo> ItemProperties { get; set; } = new List<DesignerDataPropertyInfo>();

        /// <summary>
        /// The object list.
        /// </summary>
        [DataMember(Name = "data")]
        public List<T> Data { get; set; } = new List<T>();

        /// <summary>
        /// Whether new rows can be added.
        /// </summary>
        [DataMember(Name = "canAddRows")]
        public bool CanAddRows { get; set; } = true;

        /// <summary>
        /// Whether rows can be deleted.
        /// </summary>
        [DataMember(Name = "canRemoveRows")]
        public bool CanRemoveRows { get; set; } = true;

        /// <summary>
        /// Whether rows can be moved.
        /// </summary>
        [DataMember(Name = "canMoveRows")]
        public bool CanMoveRows { get; set; } = false;

        /// <summary>
        /// Whether rows can be inserted.
        /// </summary>
        [DataMember(Name = "canInsertRows")]
        public bool CanInsertRows { get; set; } = false;

        /// <summary>
        /// Whether a confirmation should be shown when a row is about to be removed.
        /// </summary>
        [DataMember(Name = "showRemoveRowConfirmation")]
        public bool ShowRemoveRowConfirmation { get; set; } = false;

        /// <summary>
        /// The confirmation message to be displayed when a row is about to be removed.
        /// </summary>
        [DataMember(Name = "removeRowConfirmationMessage")]
        public string RemoveRowConfirmationMessage { get; set; }

        /// <summary>
        /// The label for the add new button for this table.
        /// </summary>
        [DataMember(Name = "labelForAddNewButton")]
        public string LabelForAddNewButton { get; set; }
    }
}