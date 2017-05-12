//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.ComponentModel;
using System.Resources;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Diagnostics;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database Prototype for SqlServer 2005 and later servers
    /// </summary>
    internal class DatabasePrototype90 : DatabasePrototype80SP3, IDynamicValues
    {
        /// <summary>
        /// Whether torn page detection is enabled
        /// </summary>
        [Category("Category_Automatic"),
        DisplayNameAttribute("Property_AutoUpdateStateAsync")]
        public bool AutoUpdateStatisticsAsync
        {
            get
            {
                return this.currentState.autoUpdateStatisticsAsync;
            }
            set
            {
                this.currentState.autoUpdateStatisticsAsync = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether torn page detection is enabled
        /// </summary>
        [Category("Category_Recovery"),
        DisplayNameAttribute("Property_PageVerify"),
        TypeConverter(typeof(PageVerifyTypes90))]
        public override string PageVerifyDisplay
        {
            get
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings", typeof(DatabasePrototype80).GetAssembly());
                string result = null;

                switch (this.currentState.pageVerify)
                {
                    case PageVerify.Checksum:

                        result = manager.GetString("prototype.db.prop.pageVerify.value.checksum");
                        break;

                    case PageVerify.None:

                        result = manager.GetString("prototype.db.prop.pageVerify.value.none");
                        break;

                    case PageVerify.TornPageDetection:

                        result = manager.GetString("prototype.db.prop.pageVerify.value.tornPageDetection");
                        break;
                }

                return result;
            }

            set
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings", typeof(DatabasePrototype80).GetAssembly());
                if (value == manager.GetString("prototype.db.prop.pageVerify.value.checksum"))
                {
                    this.currentState.pageVerify = PageVerify.Checksum;
                }
                else if (value == manager.GetString("prototype.db.prop.pageVerify.value.none"))
                {
                    this.currentState.pageVerify = PageVerify.None;
                }
                else
                {                
                    this.currentState.pageVerify = PageVerify.TornPageDetection;
                }

                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Use ANSI warnings
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_Trustworthy")]
        public bool Trustworthy
        {
            get
            {
                return this.currentState.trustworthy;
            }
        }

        /// <summary>
        /// Arithabort
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_DateCorrelationOptimization")]
        public bool DateCorrelationOptimization
        {
            get
            {
                return this.currentState.dateCorrelationOptimization;
            }
            set
            {
                this.currentState.dateCorrelationOptimization = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// AllowSnapshotIsolation
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_AllowSnapshotIsolation")]
        public bool AllowSnapshotIsolation
        {
            get
            {
                return this.currentState.allowSnapshotIsolation;
            }
            set
            {
                this.currentState.allowSnapshotIsolation = value;
                this.NotifyObservers();
            }
        }


        /// <summary>
        /// IsReadCommittedSnapshotOn
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_IsReadCommittedSnapshotOn")]
        public bool IsReadCommittedSnapshotOn
        {
            get
            {
                return this.currentState.isReadCommittedSnapshotOn;
            }
            set
            {
                this.currentState.isReadCommittedSnapshotOn = value;
                this.NotifyObservers();
            }
        }


        /// <summary>
        /// BrokerEnabled
        /// </summary>
        [Category("Category_ServiceBroker"),
        DisplayNameAttribute("Property_BrokerEnabled")]
        public bool BrokerEnabled
        {
            get
            {
                return this.currentState.brokerEnabled;
            }
            set
            {
                this.currentState.brokerEnabled = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether contatenating a null string yields a null result
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_Parameterization")]
        [TypeConverter(typeof(ParameterizationTypes))]
        public string Parameterization
        {
            get
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings", typeof(DatabasePrototype90).GetAssembly());
                string result = this.currentState.parameterization ?
                    manager.GetString("prototype.db.prop.parameterization.value.forced") :
                    manager.GetString("prototype.db.prop.parameterization.value.simple");

                return result;
            }
            set
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings", typeof(DatabasePrototype90).GetAssembly());
                this.currentState.parameterization = (value == manager.GetString("prototype.db.prop.parameterization.value.forced"));
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Service Broker Guid
        /// </summary>
        [Category("Category_ServiceBroker"),
        DisplayNameAttribute("Property_ServiceBrokerGUID")]
        public System.Guid ServiceBrokerGuid
        {
            get
            {
                return this.currentState.serviceBrokerGuid;
            }
        }

        public DatabasePrototype90(CDataContainer context) : base(context) { }


        /// <summary>
        /// Commit property changes to the database
        /// </summary>
        /// <param name="db">The database whose properties we are changing</param>
        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);

            if (!this.Exists || (db.DatabaseOptions.AutoUpdateStatisticsAsync != this.AutoUpdateStatisticsAsync))
            {
                db.DatabaseOptions.AutoUpdateStatisticsAsync = this.AutoUpdateStatisticsAsync;
            }

            if (!this.Exists || (db.DatabaseOptions.DateCorrelationOptimization != this.DateCorrelationOptimization))
            {
                db.DatabaseOptions.DateCorrelationOptimization = this.DateCorrelationOptimization;
            }

            if (!this.Exists || (db.DatabaseOptions.IsParameterizationForced != this.currentState.parameterization))
            {
                db.DatabaseOptions.IsParameterizationForced = this.currentState.parameterization;
            }
            if (db.IsSupportedProperty("BrokerEnabled"))
            {
                if (!this.Exists || (db.DatabaseOptions.BrokerEnabled != this.currentState.brokerEnabled))
                {
                    db.DatabaseOptions.BrokerEnabled = this.currentState.brokerEnabled;
                }
            }

            if (!this.Exists || (db.IsReadCommittedSnapshotOn != this.IsReadCommittedSnapshotOn))
            {
                db.IsReadCommittedSnapshotOn = this.IsReadCommittedSnapshotOn;
            }
        }

        #region IDynamicValues Members

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            TypeConverter.StandardValuesCollection result = null;

            if (context.PropertyDescriptor.Name == "Parameterization")
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings", typeof(DatabasePrototype90).GetAssembly());
                List<string> standardValues = new List<string>();
                standardValues.Add(manager.GetString("prototype.db.prop.parameterization.value.forced"));
                standardValues.Add(manager.GetString("prototype.db.prop.parameterization.value.simple"));
                result = new TypeConverter.StandardValuesCollection(standardValues);
            }
            else
            {
                result = base.GetStandardValues(context);
            }

            return result;
        }

        #endregion

    }
}


