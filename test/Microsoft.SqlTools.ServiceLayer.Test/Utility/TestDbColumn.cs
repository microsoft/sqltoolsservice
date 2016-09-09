// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;

namespace Microsoft.SqlTools.ServiceLayer.Test.Utility
{
    public class TestDbColumn : DbColumn
    {
        public TestDbColumn()
        {
            base.IsLong = false;
            base.ColumnName = "Test Column";
            base.ColumnSize = 128;
            base.AllowDBNull = true;
            base.DataType = typeof(string);
            base.DataTypeName = "nvarchar";
        }
    }
}
