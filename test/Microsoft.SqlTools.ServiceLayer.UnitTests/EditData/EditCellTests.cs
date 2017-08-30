//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class EditCellTests
    {
        [Fact]
        public void ConstructNullDbCell()
        {
            // If: I construct an EditCell with a null DbCellValue
            // Then: It should throw
            Assert.Throws<ArgumentNullException>(() => new EditCell(null, true));
        }

        [Fact]
        public void ConstructValid()
        {
            // Setup: Create a DbCellValue to copy the values from
            DbCellValue source = new DbCellValue
            {
                DisplayValue = "qqq",
                IsNull = true,
                RawObject = 12
            };

            // If: I construct an EditCell with a valid DbCellValue
            EditCell ec = new EditCell(source, true);

            // Then:
            // ... The values I provided in the DbCellValue should be present
            Assert.Equal(source.DisplayValue, ec.DisplayValue);
            Assert.Equal(source.IsNull, ec.IsNull);
            Assert.Equal(source.RawObject, ec.RawObject);

            // ... The is dirty value I set should be present
            Assert.True(ec.IsDirty);
        }
    }
}