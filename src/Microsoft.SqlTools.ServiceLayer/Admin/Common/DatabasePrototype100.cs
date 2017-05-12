//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ComponentModel;
using System.Resources;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database properties for SqlServer 2008
    /// </summary>
    [TypeConverter(typeof(DynamicValueTypeConverter))]
    internal class DatabasePrototype100 : DatabasePrototype90
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
            //there is no set for user database in katmai. By default it's true.
        }

        /// <summary>
        /// Whether database is encrypted or not
        /// </summary>
        [Category("Category_State"),
        DisplayNameAttribute("Property_EncryptionEnabled")]
        public bool EncryptionEnabled
        {
            get
            {
                return this.currentState.encryptionEnabled;
            }
            set
            {
                this.currentState.encryptionEnabled = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Honor Broker Priority
        /// </summary>
        [Category("Category_ServiceBroker"),
        DisplayNameAttribute("Property_HonorBrokerPriority")]
        public bool HonorBrokerPriority
        {
            get
            {
                return this.currentState.honorBrokerPriority;
            }
        }

        [Category("Category_DatabaseScopedConfigurations")]
        [DisplayNameAttribute("Property_MaxDop")]
        public int MaxDop
        {
            get
            {
                return this.currentState.maxDop;
            }
            set
            {
                this.currentState.maxDop = value;
                this.NotifyObservers();
            }
        }

        [Category("Category_DatabaseScopedConfigurations")]
        [DisplayNameAttribute("Property_MaxDopForSecondary")]
        public int? MaxDopForSecondary
        {
            get
            {
                return this.currentState.maxDopForSecondary;
            }
            set
            {
                this.currentState.maxDopForSecondary = value;
                this.NotifyObservers();
            }
        }

        [Category("Category_DatabaseScopedConfigurations"),
        DisplayNameAttribute("Property_LegacyCardinalityEstimation")]
        public string LegacyCardinalityEstimationDisplay
        {
            get
            {
                return GetDatabaseScopedConfigDisplayText(this.currentState.legacyCardinalityEstimation);
            }
            set
            {
                this.currentState.legacyCardinalityEstimation = SetDatabaseScopedConfigHelper(value, forSecondary: false);
                this.NotifyObservers();
            }
        }

        [Category("Category_DatabaseScopedConfigurations"),
        DisplayNameAttribute("Property_LegacyCardinalityEstimationForSecondary")]
        public String LegacyCardinalityEstimationForSecondaryDisplay
        {
            get
            {
                return GetDatabaseScopedConfigDisplayText(this.currentState.legacyCardinalityEstimationForSecondary);
            }
            set
            {
                this.currentState.legacyCardinalityEstimationForSecondary = SetDatabaseScopedConfigHelper(value, forSecondary: true);
                this.NotifyObservers();
            }
        }

        [Category("Category_DatabaseScopedConfigurations"),
        DisplayNameAttribute("Property_ParameterSniffing")]
        public string ParameterSniffingDisplay
        {
            get
            {
                return GetDatabaseScopedConfigDisplayText(this.currentState.parameterSniffing);
            }
            set
            {
                this.currentState.parameterSniffing = SetDatabaseScopedConfigHelper(value, forSecondary: false);
                this.NotifyObservers();
            }
        }

        [Category("Category_DatabaseScopedConfigurations"),
        DisplayNameAttribute("Property_ParameterSniffingForSecondary")]
        public String ParameterSniffingForSecondaryDisplay
        {
            get
            {
                return GetDatabaseScopedConfigDisplayText(this.currentState.parameterSniffingForSecondary);
            }
            set
            {
                this.currentState.parameterSniffingForSecondary = SetDatabaseScopedConfigHelper(value, forSecondary: true);
                this.NotifyObservers();
            }
        }

        [Category("Category_DatabaseScopedConfigurations"),
        DisplayNameAttribute("Property_QueryOptimizerHotfixes")]
        public String QueryOptimizerHotfixesDisplay
        {
            get
            {
                return GetDatabaseScopedConfigDisplayText(this.currentState.queryOptimizerHotfixes);
            }
            set
            {
                this.currentState.queryOptimizerHotfixes = SetDatabaseScopedConfigHelper(value, forSecondary: false);
                this.NotifyObservers();
            }
        }

        [Category("Category_DatabaseScopedConfigurations"),
        DisplayNameAttribute("Property_QueryOptimizerHotfixesForSecondary")]
        public String QueryOptimizerHotfixesForSecondaryDisplay
        {
            get
            {
                return GetDatabaseScopedConfigDisplayText(this.currentState.queryOptimizerHotfixesForSecondary);
            }
            set
            {
                this.currentState.queryOptimizerHotfixesForSecondary = SetDatabaseScopedConfigHelper(value, forSecondary: true);
                this.NotifyObservers();
            }
        }

        public DatabasePrototype100(CDataContainer context) : base(context) { }

        /// <summary>
        /// Commit changes to the database
        /// </summary>
        /// <param name="db">The database whose properties we are changing</param>
        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);
            if (!this.Exists || this.originalState.encryptionEnabled != this.currentState.encryptionEnabled)
            {
                db.EncryptionEnabled = this.currentState.encryptionEnabled;
            }

            // Check if we support database scoped configurations in this database. Since these were all added at the same time,
            // only check if MaxDop is supported rather than each individual property.
            if (db.IsSupportedProperty("MaxDop"))
            {
                if (!this.Exists || (db.MaxDop != this.MaxDop))
                {
                    db.MaxDop = this.MaxDop;
                }

                if (!this.Exists || (db.MaxDopForSecondary != this.MaxDopForSecondary))
                {
                    db.MaxDopForSecondary = this.MaxDopForSecondary;
                }

                if (!this.Exists || (db.LegacyCardinalityEstimation != this.currentState.legacyCardinalityEstimation))
                {
                    db.LegacyCardinalityEstimation = this.currentState.legacyCardinalityEstimation;
                }

                if (!this.Exists || (db.LegacyCardinalityEstimationForSecondary != this.currentState.legacyCardinalityEstimationForSecondary))
                {
                    db.LegacyCardinalityEstimationForSecondary = this.currentState.legacyCardinalityEstimationForSecondary;
                }

                if (!this.Exists || (db.ParameterSniffing != this.currentState.parameterSniffing))
                {
                    db.ParameterSniffing = this.currentState.parameterSniffing;
                }

                if (!this.Exists || (db.ParameterSniffingForSecondary != this.currentState.parameterSniffingForSecondary))
                {
                    db.ParameterSniffingForSecondary = this.currentState.parameterSniffingForSecondary;
                }

                if (!this.Exists || (db.QueryOptimizerHotfixes != this.currentState.queryOptimizerHotfixes))
                {
                    db.QueryOptimizerHotfixes = this.currentState.queryOptimizerHotfixes;
                }

                if (!this.Exists || (db.QueryOptimizerHotfixesForSecondary != this.currentState.queryOptimizerHotfixesForSecondary))
                {
                    db.QueryOptimizerHotfixesForSecondary = this.currentState.queryOptimizerHotfixesForSecondary;
                }
            }
        }

        #region Helper Methods

        /// <summary>
        /// Gets the display text for a database scoped configuration setting.
        /// </summary>
        /// <param name="onOffValue">The database scoped configuration setting value.</param>
        /// <returns>A string from the resource manager representing the value.</returns>
        private string GetDatabaseScopedConfigDisplayText(DatabaseScopedConfigurationOnOff onOffValue)
        {
            ResourceManager manager = new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings", typeof(DatabasePrototype).GetAssembly());
            string result = null;

            switch (onOffValue)
            {
                case DatabaseScopedConfigurationOnOff.Off:
                    result = manager.GetString("prototype.db.prop.databasescopedconfig.value.off");
                    break;

                case DatabaseScopedConfigurationOnOff.On:
                    result = manager.GetString("prototype.db.prop.databasescopedconfig.value.on");
                    break;

                case DatabaseScopedConfigurationOnOff.Primary:
                    result = manager.GetString("prototype.db.prop.databasescopedconfig.value.primary");
                    break;
            }

            return result;
        }

        /// <summary>
        /// Translates a string to a database scoped configuration enum value for the set method.
        /// </summary>
        /// <param name="displayText">The display text.</param>
        /// <param name="forSecondary">Whether this is for a secondary in which case "PRIMARY" is allowable.</param>
        /// <returns>The database scoped configuration enum value that matches the display text.</returns>
        private DatabaseScopedConfigurationOnOff SetDatabaseScopedConfigHelper(string displayText, bool forSecondary)
        {
            ResourceManager manager = new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings", typeof(DatabasePrototype).GetAssembly());

            if (displayText == manager.GetString("prototype.db.prop.databasescopedconfig.value.off"))
            {
                return DatabaseScopedConfigurationOnOff.Off;
            }
            else if (displayText == manager.GetString("prototype.db.prop.databasescopedconfig.value.on") || !forSecondary)
            {            
                return DatabaseScopedConfigurationOnOff.On;
            }
            else
            {               
                return DatabaseScopedConfigurationOnOff.Primary;
            }
        }

        #endregion
    }
}
