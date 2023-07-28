//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database properties for SqlServer 2016
    /// </summary>
    internal class DatabasePrototype130 : DatabasePrototype110
    {
        /// <summary>
        /// Database properties for SqlServer 2016 class constructor
        /// </summary>
        public DatabasePrototype130(CDataContainer context)
            : base(context)
        {
        }

        [Category("Category_DatabaseScopedConfigurations")]
        public DatabaseScopedConfigurationCollection DatabaseScopedConfiguration
        {
            get
            {
                return this.currentState.databaseScopedConfigurations;
            }
            set
            {
                this.currentState.databaseScopedConfigurations = value;
                this.NotifyObservers();
            }
        }

        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);

            for(int i = 0; i < db.DatabaseScopedConfigurations.Count; i++)
            {
                if (db.DatabaseScopedConfigurations[i].Value != this.currentState.databaseScopedConfigurations[i].Value)
                {
                    db.DatabaseScopedConfigurations[i].Value = this.currentState.databaseScopedConfigurations[i].Value;
                }
                // Below propertiesoption does not allow updates for the secondaries replica while this option is only allowed to be set for the primary.
                // ELEVATE_ONLINE(11), ELEVATE_RESUMABLE(12), GLOBAL_TEMPORARY_TABLE_AUTO_DROP(21), IDENTITY_CACHE(6), PAUSED_RESUMABLE_INDEX_ABORT_DURATION_MINUTES(25)
                if (db.DatabaseScopedConfigurations[i].ValueForSecondary != this.currentState.databaseScopedConfigurations[i].ValueForSecondary
                    && !new[] { 6, 11, 12, 21, 25 }.Contains(db.DatabaseScopedConfigurations[i].Id))
                {
                    db.DatabaseScopedConfigurations[i].ValueForSecondary = this.currentState.databaseScopedConfigurations[i].ValueForSecondary;
                }
            }
        }
    }
}
