//------------------------------------------------------------------------------
// <copyright file="ISqlModelUpdatingService.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater;

namespace Microsoft.Data.Tools.Schema.Sql.DesignServices
{
    /// <summary>
    /// Service that provides model change operations
    /// and updates underlying script sources
    /// 
    /// General operations
    /// </summary>
    internal partial interface ISqlModelUpdatingService
    {
        event EventHandler<BeforeModelUpdateEventArgs> BeforeUpdatingScripts;

        event EventHandler<BeforeResolveChangesEventArgs> BeforeResolveChanges;

        void RenameElement(ISqlModelElement modelElement, string newName);
        void RenameElement(ISqlModelElement modelElement, string newName, uint undoScope);

        void AddExtendedProperty(ISqlExtendedPropertyHost propertyHost, string propertyName, string value);
        void SetExtendedProperty(SqlExtendedProperty extendedProperty, string value);
        void DeleteExtendedProperty(SqlExtendedProperty extendedProperty);
        void SetDescriptionForElement(ISqlExtendedPropertyHost propertyHost, string value);

        bool IsProcessingScriptChanges { get; } 
    }
}
