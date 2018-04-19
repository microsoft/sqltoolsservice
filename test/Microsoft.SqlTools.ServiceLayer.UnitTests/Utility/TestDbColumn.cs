// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    public class TestDbColumn : DbColumn
    {
        #region Overridden Properties

        public new bool AllowDBNull
        {
            get { return base.AllowDBNull.HasTrue(); }
            set { base.AllowDBNull = value; }
        }

        public new string ColumnName
        {
            get { return base.ColumnName; }
            set { base.ColumnName = value; }
        }

        public new int ColumnSize
        {
            get { return base.ColumnSize ?? -1; }
            set { base.ColumnSize = value; }
        }

        public new Type DataType
        {
            get { return base.DataType; }
            set { base.DataType = value; }
        }

        public new string DataTypeName
        {
            get { return base.DataTypeName; }
            set { base.DataTypeName = value; }
        }

        public new bool IsAutoIncrement
        {
            get { return base.IsAutoIncrement.HasTrue(); }
            set { base.IsAutoIncrement = value; }
        }

        public new bool IsLong
        {
            get { return base.IsLong.HasTrue(); }
            set { base.IsLong = value; }
        }

        public new bool IsIdentity
        {
            get { return base.IsIdentity.HasTrue(); }
            set { base.IsIdentity = value; }
        }

        public new bool IsKey
        {
            get { return base.IsKey.HasTrue(); }
            set { base.IsKey = value; }
        }

        public new int NumericScale
        {
            get { return base.NumericScale ?? -1; }
            set { base.NumericScale = value; }
        }

        #endregion

        public TestDbColumn()
        {
            base.ColumnName = "col";
        }

        /// <summary>
        /// Constructs a basic DbColumn that is an NVARCHAR(128) NULL
        /// </summary>
        /// <param name="columnName">Name of the column</param>
        public TestDbColumn(string columnName, int? columnOrdinal = null)
        {
            base.IsLong = false;
            base.ColumnName = columnName;
            base.ColumnSize = 128;
            base.AllowDBNull = true;
            base.DataType = typeof(string);
            base.DataTypeName = "nvarchar";
            base.ColumnOrdinal = columnOrdinal;
        }
    }
}
