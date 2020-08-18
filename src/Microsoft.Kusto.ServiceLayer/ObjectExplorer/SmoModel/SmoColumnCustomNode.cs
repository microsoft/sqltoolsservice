//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    /// <summary>
    /// Custom name for Columns
    /// </summary>
    internal partial class ColumnsChildFactory : DataSourceChildFactoryBase
    {
        private readonly Lazy<List<NodeSmoProperty>> smoPropertiesLazy = new Lazy<List<NodeSmoProperty>>(() => new List<NodeSmoProperty>
        {
            new NodeSmoProperty
            {
                Name = "Computed",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "IsColumnSet",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "Nullable",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "DataType",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "InPrimaryKey",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "IsForeignKey",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "SystemType",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "Length",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "NumericPrecision",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "NumericScale",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "XmlSchemaNamespaceSchema",
                ValidFor = ValidForFlag.NotSqlDw
            },
            new NodeSmoProperty
            {
                Name = "XmlSchemaNamespace",
                ValidFor = ValidForFlag.NotSqlDw
            },
            new NodeSmoProperty
            {
                Name = "XmlDocumentConstraint",
                ValidFor = ValidForFlag.NotSqlDw
            }
        });

        public override IEnumerable<NodeSmoProperty> SmoProperties => smoPropertiesLazy.Value;
    }
}
