//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.ComponentModel;
using System.Resources;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Diagnostics;
using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.Management;

namespace Microsoft.Kusto.ServiceLayer.Admin
{
    /// <summary>
    /// Database Prototype for SqlServer 2000 and later servers
    /// </summary>
    internal class DatabasePrototype80 : DatabasePrototype, IDynamicValues
    {
        /// <summary>
        /// Whether the database is read-only
        /// </summary>
        [
            Category("Category_State"),
            DisplayNameAttribute("Property_ReadOnly")
        ]
        public bool IsReadOnly
        {
            get
            {
                return this.currentState.isReadOnly;
            }
            set
            {
                this.currentState.isReadOnly = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether torn page detection is enabled
        /// </summary>
        [Category("Category_Recovery"),
        DisplayNameAttribute("Property_PageVerify"),
        TypeConverter(typeof(PageVerifyTypes80))]
        public virtual string PageVerifyDisplay
        {
            get
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
                string result = null;

                switch (this.currentState.pageVerify)
                {
                    case PageVerify.Checksum:

                        result = manager.GetString("prototype_db_prop_pageVerify_value_checksum");
                        break;

                    case PageVerify.None:

                        result = manager.GetString("prototype_db_prop_pageVerify_value_none");
                        break;

                    case PageVerify.TornPageDetection:

                        result = manager.GetString("prototype.db.prop.pageVerify.value.tornPageDetection");
                        break;
                }

                return result;
            }

            set
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
                if (value == manager.GetString("prototype_db_prop_pageVerify_value_checksum"))
                {
                    this.currentState.pageVerify = PageVerify.Checksum;
                }
                else if (value == manager.GetString("prototype_db_prop_pageVerify_value_none"))
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
        /// ANSI Padding enabled
        /// </summary>
        [Category("Category_Misc"),
       DisplayNameAttribute("Property_ANSIPadding")]
        public bool AnsiPadding
        {
            get
            {
                return this.currentState.ansiPadding;
            }
            set
            {
                this.currentState.ansiPadding = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Use ANSI warnings
        /// </summary>
        [Category("Category_Misc"),
       DisplayNameAttribute("Property_ANSIWarnings")]
        public bool AnsiWarnings
        {
            get
            {
                return this.currentState.ansiWarnings;
            }
            set
            {
                this.currentState.ansiWarnings = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Arithabort
        /// </summary>
        [Category("Category_Misc"),
       DisplayNameAttribute("Property_ArithAbort")]
        public bool Arithabort
        {
            get
            {
                return this.currentState.arithabort;
            }
            set
            {
                this.currentState.arithabort = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether contatenating a null string yields a null result
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_ConcatNullYieldsNull")]
        public bool ConcatNullYieldsNull
        {
            get
            {
                return this.currentState.concatNullYieldsNull;
            }
            set
            {
                this.currentState.concatNullYieldsNull = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Numeric Roundabout
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_NumericRoundAbort")]
        public bool NumericRoundAbort
        {
            get
            {
                return this.currentState.numericRoundAbort;
            }
            set
            {
                this.currentState.numericRoundAbort = value;
                this.NotifyObservers();
            }
        }


        public DatabasePrototype80(CDataContainer context) : base(context) { }

        /// <summary>
        /// Commit property changes to the database
        /// </summary>
        /// <param name="db">The database whose properties we are changing</param>
        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);

            ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());

            // never set the real database collation to "<server default>" - there is no
            // real collation with that name. "<server default>" is only valid for new
            // databases and just means "don't set the collation".
            if ((!this.Exists || (this.Exists && (db.Collation != this.Collation))) &&
                (this.originalState.defaultCollation != this.Collation))
            {
                db.Collation = this.Collation;
            }

            if (!this.Exists || (db.DatabaseOptions.AnsiPaddingEnabled != this.AnsiPadding))
            {
                db.DatabaseOptions.AnsiPaddingEnabled = this.AnsiPadding;
            }

            if (!this.Exists || (db.DatabaseOptions.AnsiWarningsEnabled != this.AnsiWarnings))
            {
                db.DatabaseOptions.AnsiWarningsEnabled = this.AnsiWarnings;
            }

            if (!this.Exists || (db.DatabaseOptions.ArithmeticAbortEnabled != this.Arithabort))
            {
                db.DatabaseOptions.ArithmeticAbortEnabled = this.Arithabort;
            }

            if (!this.Exists || (db.DatabaseOptions.ConcatenateNullYieldsNull != this.ConcatNullYieldsNull))
            {
                db.DatabaseOptions.ConcatenateNullYieldsNull = this.ConcatNullYieldsNull;
            }

            if (db.IsSupportedProperty("PageVerify"))
            {
                if (!this.Exists || (db.DatabaseOptions.PageVerify != this.currentState.pageVerify))
                {
                    db.DatabaseOptions.PageVerify = this.currentState.pageVerify;
                }
            }

            if (!this.Exists || (db.DatabaseOptions.NumericRoundAbortEnabled != this.NumericRoundAbort))
            {
                db.DatabaseOptions.NumericRoundAbortEnabled = this.NumericRoundAbort;
            }

            if (!this.Exists || (db.DatabaseOptions.ReadOnly != this.IsReadOnly))
            {
                db.DatabaseOptions.ReadOnly = this.IsReadOnly;
            }
        }


        #region IDynamicValues Members

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            TypeConverter.StandardValuesCollection result = null;

            if (context.PropertyDescriptor.Name == "PageVerifyDisplay")
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
                List<string> standardValues = new List<string>();

                if (this.IsYukonOrLater)
                {
                    standardValues.Add(manager.GetString("prototype_db_prop_pageVerify_value_checksum"));
                }

                standardValues.Add(manager.GetString("prototype_db_prop_pageVerify_value_tornPageDetection"));
                standardValues.Add(manager.GetString("prototype_db_prop_pageVerify_value_none"));

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


