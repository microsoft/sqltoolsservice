//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.ComponentModel;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.Kusto.ServiceLayer.Management;

namespace Microsoft.Kusto.ServiceLayer.Admin
{
    /// <summary>
    /// Database Prototype for SqlServer 2000 SP3 and later servers
    /// </summary>
    [TypeConverter(typeof(DynamicValueTypeConverter))]
    internal class DatabasePrototype80SP3 : DatabasePrototype80
    {
        /// <summary>
        /// ANSI Padding enabled
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_DBChaining")]
        public bool DbChaining
        {
            get
            {
                return this.currentState.dbChaining;
            }
        }


        public DatabasePrototype80SP3(CDataContainer context) : base(context) { }
    }
}
