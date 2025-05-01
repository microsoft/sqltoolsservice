//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    internal class SmoColumnCustomNodeHelperTests
    {
        [Test]
        public void ShouldCalculateTypeLabels()
        {
            var cases = new[]
            {
                // Numbers
                new { Type = DataType.Int, Label = "int" },
                new { Type = DataType.BigInt, Label = "bigint" },
                new { Type = DataType.SmallInt, Label = "smallint" },
                new { Type = DataType.TinyInt, Label = "tinyint" },
                new { Type = DataType.Bit, Label = "bit" },
                new { Type = DataType.Decimal(10, 2), Label = "decimal(2,10)" }, // precision then scale is standard, opposite order from the constructor
                new { Type = DataType.Float, Label = "float" },
                new { Type = DataType.Real, Label = "real" },
                new { Type = DataType.Money, Label = "money" },
                new { Type = DataType.SmallMoney, Label = "smallmoney" },

                // Binary types
                new { Type = DataType.Binary(10), Label = "binary(10)" },
                new { Type = DataType.VarBinary(10), Label = "varbinary(10)" },
                new { Type = DataType.VarBinaryMax, Label = "varbinary(max)" },

                // String types
                new { Type = DataType.Char(10), Label = "char(10)" },
                new { Type = DataType.NChar(10), Label = "nchar(10)" },
                new { Type = DataType.NVarChar(10), Label = "nvarchar(10)" },
                new { Type = DataType.NVarCharMax, Label = "nvarchar(max)" },
                new { Type = DataType.VarChar(20), Label = "varchar(20)" },
                new { Type = DataType.VarCharMax, Label = "varchar(max)" },

                // Date Types
                new { Type = DataType.DateTime, Label = "datetime" },
                new { Type = DataType.Date, Label = "date" },
                new { Type = DataType.DateTime2(7), Label = "datetime2(7)" },
                new { Type = DataType.DateTimeOffset(5), Label = "datetimeoffset(5)" },
                new { Type = DataType.Time(3), Label = "time(3)" },

                // Specialty types
                new { Type = DataType.Vector(17), Label = "vector(17)" },
            };

            foreach (var testCase in cases)
            {
                string label = SmoColumnCustomNodeHelper.GetTypeSpecifierLabel(testCase.Type, uddts: null);
                Assert.That(label, Is.EqualTo(testCase.Label), $"Expected label to be {testCase.Label}");
            }
        }
    }
}