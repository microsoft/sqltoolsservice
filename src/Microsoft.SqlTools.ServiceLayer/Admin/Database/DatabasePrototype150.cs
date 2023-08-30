//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database properties for SqlServer 2019
    /// </summary>
    internal class DatabasePrototype150 : DatabasePrototype140
    {
        /// <summary>
        /// Database properties for SqlServer 2019 class constructor
        /// </summary>
        public DatabasePrototype150(CDataContainer context)
            : base(context)
        {
        }

        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);
        }
    }
}
