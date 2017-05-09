//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Subtye for keys
    /// </summary>
    internal partial class KeysChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeSubType(object context)
        {
            return IndexCustomeNodeHelper.GetSubType(context);
        }
    }

    /// <summary>
    /// Sub types and custom name for indexes
    /// </summary>
    internal partial class IndexesChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeSubType(object context)
        {
            return IndexCustomeNodeHelper.GetSubType(context);
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
        public override string GetNodeSubType(object context)
        {
            return IndexCustomeNodeHelper.GetSubType(context);
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
