//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    /// <summary>
    /// Subtye for keys
    /// </summary>
    internal partial class KeysChildFactory : DataSourceChildFactoryBase
    {
        public override string GetNodeSubType(object objectMetadata, QueryContext oeContext)
        {
            return IndexCustomeNodeHelper.GetSubType(objectMetadata);
        }
    }

    /// <summary>
    /// Sub types and custom name for indexes
    /// </summary>
    internal partial class IndexesChildFactory : DataSourceChildFactoryBase
    {
        private readonly Lazy<List<NodeSmoProperty>> smoPropertiesLazy = new Lazy<List<NodeSmoProperty>>(() => new List<NodeSmoProperty>
        {
            new NodeSmoProperty
            {
                Name = "IsUnique",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "IsClustered",
                ValidFor = ValidForFlag.All
            },
            new NodeSmoProperty
            {
                Name = "IndexKeyType",
                ValidFor = ValidForFlag.All
            }
        });

        public override IEnumerable<NodeSmoProperty> SmoProperties => smoPropertiesLazy.Value;

        public override string GetNodeSubType(object objectMetadata, QueryContext oeContext)
        {
            return IndexCustomeNodeHelper.GetSubType(objectMetadata);
        }

        public override string GetNodeCustomName(object objectMetadata, QueryContext oeContext)
        {
            return IndexCustomeNodeHelper.GetCustomLabel(objectMetadata);
        }
    }

    /// <summary>
    /// sub type for UserDefinedTableTypeKeys
    /// </summary>
    internal partial class UserDefinedTableTypeKeysChildFactory : DataSourceChildFactoryBase
    {
        public override string GetNodeSubType(object objectMetadata, QueryContext oeContext)
        {
            return IndexCustomeNodeHelper.GetSubType(objectMetadata);
        }
    }

    internal static class IndexCustomeNodeHelper
    {
        internal static string GetCustomLabel(object context)
        {
            Index index = context as Index;
            if (index != null)
            {
                string name = index.Name;
                string unique = index.IsUnique ? SR.UniqueIndex_LabelPart : SR.NonUniqueIndex_LabelPart;
                string clustered = index.IsClustered ? SR.ClusteredIndex_LabelPart : SR.NonClusteredIndex_LabelPart;
                name = name + $" ({unique}, {clustered})";
                return name;
            }
            return string.Empty;

        }

        internal static string GetSubType(object context)
        {

            Index index = context as Index;
            if (index != null)
            {
                switch (index.IndexKeyType)
                {
                    case IndexKeyType.DriPrimaryKey:
                        return "PrimaryKey";
                    case IndexKeyType.DriUniqueKey:
                        return "UniqueKey";
                }

            }

            ForeignKey foreignKey = context as ForeignKey;
            if (foreignKey != null)
            {
                return "ForeignKey";
            }

            return string.Empty;
        }
    }
}
