//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    public class DataSourceChildFactoryBase : ChildFactory
    {
        private IEnumerable<NodeSmoProperty> smoProperties;
        public override IEnumerable<string> ApplicableParents()
        {
            return null;
        }

        public override IEnumerable<NodeSmoProperty> SmoProperties
        {
            get
            {
                return Enumerable.Empty<NodeSmoProperty>();
            }
        }

        internal IEnumerable<NodeSmoProperty> CachedSmoProperties
        {
            get
            {
                return smoProperties == null ? SmoProperties : smoProperties;
            }
        }
    }
}
