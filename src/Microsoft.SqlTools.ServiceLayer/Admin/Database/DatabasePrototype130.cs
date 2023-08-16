﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database properties for SqlServer 2016
    /// </summary>
    internal class DatabasePrototype130 : DatabasePrototype110
    {
        // Properties that doen't support secondary value updates
        // More info here: https://learn.microsoft.com/en-us/sql/t-sql/statements/alter-database-scoped-configuration-transact-sql?view=sql-server-ver16
        // The secondaryValUnsupportedPropsSet containst the configuration Ids of the below properties
        // IDENTITY_CACHE(6)
        // ELEVATE_ONLINE(11)
        // ELEVATE_RESUMABLE(12)
        // GLOBAL_TEMPORARY_TABLE_AUTO_DROP(21)
        // PAUSED_RESUMABLE_INDEX_ABORT_DURATION_MINUTES(25)
        private static readonly HashSet<int> secondaryValUnsupportedPropsSet = new HashSet<int> { 6, 11, 12, 21, 25 };

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

            for (int i = 0; i < db.DatabaseScopedConfigurations.Count; i++)
            {
                if (db.DatabaseScopedConfigurations[i].Value != this.currentState.databaseScopedConfigurations[i].Value)
                {
                    db.DatabaseScopedConfigurations[i].Value = this.currentState.databaseScopedConfigurations[i].Value;
                }

                // Configurations that are not allowed secondary replicas are excluded.
                if (db.DatabaseScopedConfigurations[i].ValueForSecondary != this.currentState.databaseScopedConfigurations[i].ValueForSecondary
                    && !secondaryValUnsupportedPropsSet.Contains(db.DatabaseScopedConfigurations[i].Id))
                {
                    db.DatabaseScopedConfigurations[i].ValueForSecondary = this.currentState.databaseScopedConfigurations[i].ValueForSecondary;
                }
            }
        }
    }
}
