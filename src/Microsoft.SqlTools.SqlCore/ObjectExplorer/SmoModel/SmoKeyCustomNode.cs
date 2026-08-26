//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;
using Index = Microsoft.SqlServer.Management.Smo.Index;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Subtye for keys
    /// </summary>
    internal partial class KeysChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            return IndexCustomeNodeHelper.GetSubType(smoObject);
        }
    }

    /// <summary>
    /// Sub types and custom name for indexes
    /// </summary>
    internal partial class IndexesChildFactory : SmoChildFactoryBase
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

        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            return IndexCustomeNodeHelper.GetSubType(smoObject);
        }

        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            return IndexCustomeNodeHelper.GetCustomLabel(smoObject);
        }
    }

    /// <summary>
    /// sub type for UserDefinedTableTypeKeys
    /// </summary>
    internal partial class UserDefinedTableTypeKeysChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            return IndexCustomeNodeHelper.GetSubType(smoObject);
        }
    }

    internal static class IndexCustomeNodeHelper
    {
        internal static string GetCustomLabel(object context)
        {
            Index index = context as Index;
            if (index != null)
            {
                return BuildIndexLabel(index.Name, index.IsUnique, index.IsClustered, index.IndexType);
            }
            return string.Empty;
        }

        internal static string BuildIndexLabel(string name, bool isUnique, bool isClustered, IndexType indexType)
        {
            string unique = isUnique ? SR.UniqueIndex_LabelPart : SR.NonUniqueIndex_LabelPart;
            string type = GetIndexTypeLabel(isClustered, indexType);
            return name + $" ({unique}, {type})";
        }

        private static string GetIndexTypeLabel(bool isClustered, IndexType indexType)
        {
            switch (indexType)
            {
                case IndexType.ClusteredColumnStoreIndex:
                    return SR.ClusteredColumnStoreIndex_LabelPart;
                case IndexType.NonClusteredColumnStoreIndex:
                    return SR.NonClusteredColumnStoreIndex_LabelPart;
                default:
                    return isClustered ? SR.ClusteredIndex_LabelPart : SR.NonClusteredIndex_LabelPart;
            }
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
