//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using System.ComponentModel;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database properties for SqlServer 2022
    /// </summary>
    internal class DatabasePrototype160 : DatabasePrototype140
    {
        /// <summary>
        /// Database properties for SqlServer 2022 class constructor
        /// </summary>
        public DatabasePrototype160(CDataContainer context)
            : base(context)
        {
        }

        [Category("Category_Ledger"),
        DisplayNameAttribute("Property_IsLedgerDatabase")]
        public bool IsLedger
        {
            get {
                return this.currentState.isLedger;
            }
            set
            {
                this.currentState.isLedger = value;
                this.NotifyObservers();
            }
        }

        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);

            if (db.IsSupportedProperty("IsLedger"))
            {
                // Ledger can only be set on a new database, it is read-only after creation
                if (!this.Exists)
                {
                    db.IsLedger = this.IsLedger;
                }
            }
        }
    }
}
