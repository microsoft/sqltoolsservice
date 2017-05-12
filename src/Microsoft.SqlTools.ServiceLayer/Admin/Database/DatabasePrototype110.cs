//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.ComponentModel;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database properties for SqlServer 2011
    /// </summary>
    [TypeConverter(typeof(DynamicValueTypeConverter))]
    internal class DatabasePrototype110 : DatabasePrototype100
    {
        /// <summary>
        /// Database compatibility level
        /// </summary>
        [Browsable(false)]
        public ContainmentType DatabaseContainmentType
        {
            get
            {
                return this.currentState.databaseContainmentType;
            }
            set
            {
                this.currentState.databaseContainmentType = value;
                this.NotifyObservers();
            }
        }

        [Category("Category_ContainedDatabases"),
        DisplayNameAttribute("Property_DefaultFullTextLanguageLcid")]
        public int DefaultFullTextLanguageLcid
        {
            get
            {
                return this.currentState.defaultFulltextLanguageLcid;
            }
            set
            {
                this.currentState.defaultFulltextLanguageLcid = value;
                this.NotifyObservers();
            }
        }

        [Category("Category_ContainedDatabases"),
        DisplayNameAttribute("Property_NestedTriggersEnabled")]
        public bool NestedTriggersEnabled
        {
            get
            {
                return this.currentState.nestedTriggersEnabled;
            }
            set
            {
                this.currentState.nestedTriggersEnabled = value;
                this.NotifyObservers();
            }
        }

        [Category("Category_ContainedDatabases"),
        DisplayNameAttribute("Property_TransformNoiseWords")]
        public bool TransformNoiseWords
        {
            get
            {
                return this.currentState.transformNoiseWords;
            }
            set
            {
                this.currentState.transformNoiseWords = value;
                this.NotifyObservers();
            }
        }

        [Category("Category_ContainedDatabases"),
        DisplayNameAttribute("Property_TwoDigitYearCutoff")]
        public int TwoDigitYearCutoff
        {
            get
            {
                return this.currentState.twoDigitYearCutoff;
            }
            set
            {
                this.currentState.twoDigitYearCutoff = value;
                this.NotifyObservers();
            }
        }

        [Category("Category_Recovery"),
        DisplayNameAttribute("Property_TargetRecoveryTime")]
        public int TargetRecoveryTime
        {
            get
            {
                return this.currentState.targetRecoveryTime;
            }
            set
            {
                this.currentState.targetRecoveryTime = value;
                this.NotifyObservers();
            }
        }

        [Category("Category_Misc"),
        DisplayNameAttribute("Property_DelayedDurability")]
        public DelayedDurability DelayedDurability
        {
            get
            {
                return this.currentState.delayedDurability;
            }
            set
            {
                this.currentState.delayedDurability = value;
                this.NotifyObservers();
            }
        }

        public DatabasePrototype110(CDataContainer context)
            : base(context)
        {
        }

        /// <summary>
        /// Commit changes to the database
        /// </summary>
        /// <param name="db">The database whose properties we are changing</param>
        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);

            if (db.IsSupportedProperty("ContainmentType"))
            {
                if (!this.Exists || (db.ContainmentType != this.DatabaseContainmentType))
                {
                    db.ContainmentType = this.DatabaseContainmentType;
                }

                if (this.DatabaseContainmentType != ContainmentType.None)
                {
                    if (!this.Exists || (db.DefaultFullTextLanguage.Lcid != this.DefaultFullTextLanguageLcid))
                    {
                        db.DefaultFullTextLanguage.Lcid = this.DefaultFullTextLanguageLcid;
                    }

                    if (!this.Exists || (db.NestedTriggersEnabled != this.NestedTriggersEnabled))
                    {
                        db.NestedTriggersEnabled = this.NestedTriggersEnabled;
                    }

                    if (!this.Exists || (db.TransformNoiseWords != this.TransformNoiseWords))
                    {
                        db.TransformNoiseWords = this.TransformNoiseWords;
                    }

                    if (!this.Exists || (db.TwoDigitYearCutoff != this.TwoDigitYearCutoff))
                    {
                        db.TwoDigitYearCutoff = this.TwoDigitYearCutoff;
                    }
                }
            }

            if (db.IsSupportedProperty("TargetRecoveryTime"))
            {
                if (!this.Exists || (db.TargetRecoveryTime != this.TargetRecoveryTime))
                {
                    db.TargetRecoveryTime = this.TargetRecoveryTime;
                }
            }

            if (db.IsSupportedProperty("DelayedDurability"))
            {
                if (!this.Exists || (db.DelayedDurability != this.DelayedDurability))
                {
                    db.DelayedDurability = this.DelayedDurability;
                }
            }
        }
    }
}
