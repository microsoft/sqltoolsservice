//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.ComponentModel;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.Kusto.ServiceLayer.Management;

namespace Microsoft.Kusto.ServiceLayer.Admin
{
    /// <summary>
    /// Database Prototype for SqlServer 2005 Enterprise SP2 and later servers
    /// </summary>
    [TypeConverter(typeof(DynamicValueTypeConverter))]
    internal class DatabasePrototype90EnterpriseSP2 : DatabasePrototype90
    {
        /// <summary>
        /// Whether vardecimal compression is enabled on the server
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_VarDecimalEnabled")]
        public bool VarDecimalEnabled
        {
            get
            {
                return this.currentState.varDecimalEnabled;
            }

            set
            {
                this.currentState.varDecimalEnabled = value;
                this.NotifyObservers();
            }
        }

        public DatabasePrototype90EnterpriseSP2(CDataContainer context) : base(context) { }


        /// <summary>
        /// Commit property changes to the database
        /// </summary>
        /// <param name="db">The database whose properties we are changing</param>
        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);

            // changing decimal compression status is very expensive, so 
            // only set a value for vardecimal compression if its value has changed
            if (this.originalState.varDecimalEnabled != this.currentState.varDecimalEnabled)
            {
                db.IsVarDecimalStorageFormatEnabled = this.currentState.varDecimalEnabled;
            }
        }
    }
}


