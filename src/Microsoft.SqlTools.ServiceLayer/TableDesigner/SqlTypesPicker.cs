//------------------------------------------------------------------------------
// <copyright file="SqlTypesPicker.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Data.Relational.Design.Controls;
using Microsoft.Data.Relational.Design.Properties;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;

namespace Microsoft.Data.Relational.Design.Table
{
    internal class SqlTypePickerItem : PickerItemBase
    {
        public enum SqlTypeCategory
        {
            SqlType,
            ClrType,
            UddtType
        }

        private static Dictionary<SqlTypeCategory, string> _categoryDisplayStringMaps;

        static SqlTypePickerItem()
        {
            _categoryDisplayStringMaps = new Dictionary<SqlTypeCategory, string>();
            _categoryDisplayStringMaps.Add(SqlTypeCategory.SqlType, string.Empty);
            _categoryDisplayStringMaps.Add(SqlTypeCategory.ClrType, Resources.StrClrTypes);
            _categoryDisplayStringMaps.Add(SqlTypeCategory.UddtType, Resources.StrUddtTypes);
        }

        private SqlTypePickerItem(SqlType type, SqlTypeCategory cat, SqlTypeDisplayBase sqlTypeDisplay)
            : base(null, _categoryDisplayStringMaps[cat])
        {
            this.SqlType = type;
            this.SqlCategory = cat;
            this.SqlTypeDisplay = sqlTypeDisplay;
        }

        public static SqlTypePickerItem Create(SqlType type, SqlTypeCategory cat, SqlTypeDisplayBase sqlTypeDisplayNameFactory)
        {
            SqlTypePickerItem instance = new SqlTypePickerItem(type, cat, sqlTypeDisplayNameFactory);
            instance.Initialize();
            return instance;
        }

        public SqlTypeCategory SqlCategory
        {
            get;
            private set;
        }

        public SqlType SqlType
        {
            get;
            private set;
        }

        public SqlTypeDisplayBase SqlTypeDisplay
        {
            get;
            private set;
        }

        public override void Initialize()
        {
            this.Name = this.SqlTypeDisplay.DisplayName;
            base.Initialize();
        }
    }

    internal class SqlTypesComparer : IComparer<SqlTypePickerItem>
    {
        public int Compare(SqlTypePickerItem x, SqlTypePickerItem y)
        {
            int comparison = 0;

            if (x == y)
            {
                return 0;
            }
            else
            {
                SqlTypePickerItem.SqlTypeCategory xCat = x.SqlCategory;
                SqlTypePickerItem.SqlTypeCategory yCat = y.SqlCategory;

                if (xCat < yCat)
                {
                    return -1;
                }
                else if (xCat > yCat)
                {
                    return +1;
                }
            }

            comparison = string.Compare(x.Name, y.Name, StringComparison.CurrentCulture);

            return comparison;
        }
    }
}
