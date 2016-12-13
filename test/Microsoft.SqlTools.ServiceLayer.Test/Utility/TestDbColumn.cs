// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;

namespace Microsoft.SqlTools.ServiceLayer.Test.Utility
{
    public class TestDbColumn : DbColumn
    {
        public TestDbColumn(string columnName)
        {
            base.IsLong = false;
            base.ColumnName = columnName;
            base.ColumnSize = 128;
            base.AllowDBNull = true;
            base.DataType = typeof(string);
            base.DataTypeName = "nvarchar";
        }

        public TestDbColumn(string columnName, int numericScale)
            : this(columnName)
        {
            base.NumericScale = numericScale;
        }
    }
}
