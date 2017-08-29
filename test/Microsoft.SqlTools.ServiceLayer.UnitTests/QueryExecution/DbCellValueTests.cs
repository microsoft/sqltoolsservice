//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class DbCellValueTests
    {
        [Fact]
        public void ConstructValid()
        {
            // If: I construct a new DbCellValue
            DbCellValue dbc = new DbCellValue
            {
                DisplayValue = "qqq",
                IsNull = true,
                RawObject = 12
            };

            // Then: It should have the values I specified in it
            Assert.Equal("qqq", dbc.DisplayValue);
            Assert.Equal(12, dbc.RawObject);
            Assert.True(dbc.IsNull);
        }

        [Fact]
        public void CopyToNullOther()
        {
            // If: I copy a DbCellValue to null
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(() => new DbCellValue().CopyTo(null));
        }

        [Fact]
        public void CopyToValid()
        {
            // If: I copy a DbCellValue to another DbCellValue
            DbCellValue source = new DbCellValue {DisplayValue = "qqq", IsNull = true, RawObject = 12};
            DbCellValue dest = new DbCellValue();
            source.CopyTo(dest);

            // Then: The source values should be in the dest
            Assert.Equal(source.DisplayValue, dest.DisplayValue);
            Assert.Equal(source.IsNull, dest.IsNull);
            Assert.Equal(source.RawObject, dest.RawObject);
        }
    }
}