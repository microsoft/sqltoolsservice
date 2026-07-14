//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.SchemaDesigner;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SchemaDesigner
{
    /// <summary>
    /// Tests for <see cref="SchemaDesignerUtils.DeepCompareColumn"/> and
    /// <see cref="SchemaDesignerUtils.DeepCompareTable"/>, pinning that default value,
    /// computed, and identity changes mark a column (and its table) as modified.
    /// </summary>
    public class SchemaDesignerUtilsTests
    {
        private static readonly Guid ColumnId = new Guid("11111111-1111-1111-1111-111111111111");
        private static readonly Guid TableId = new Guid("22222222-2222-2222-2222-222222222222");

        private static SchemaDesignerColumn CreateColumn(Action<SchemaDesignerColumn> mutate = null)
        {
            var column = new SchemaDesignerColumn
            {
                Id = ColumnId,
                Name = "Amount",
                DataType = "decimal",
                MaxLength = null,
                Precision = 18,
                Scale = 2,
                IsPrimaryKey = false,
                IsIdentity = false,
                IdentitySeed = null,
                IdentityIncrement = null,
                IsNullable = true,
                DefaultValue = null,
                IsComputed = false,
                ComputedFormula = null,
                ComputedPersisted = null,
            };
            mutate?.Invoke(column);
            return column;
        }

        private static SchemaDesignerColumn CreateComputedColumn(Action<SchemaDesignerColumn> mutate = null)
        {
            var column = new SchemaDesignerColumn
            {
                Id = ColumnId,
                Name = "Total",
                IsNullable = true,
                IsComputed = true,
                ComputedFormula = "[Price] * [Quantity]",
                ComputedPersisted = false,
            };
            mutate?.Invoke(column);
            return column;
        }

        private static SchemaDesignerTable CreateTable(SchemaDesignerColumn column)
        {
            return new SchemaDesignerTable
            {
                Id = TableId,
                Name = "Orders",
                Schema = "dbo",
                Columns = new List<SchemaDesignerColumn> { column },
                ForeignKeys = new List<SchemaDesignerForeignKey>(),
            };
        }

        [Test]
        public void DeepCompareColumn_IdenticalColumns_AreEqual()
        {
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(CreateColumn(), CreateColumn()), Is.True);
        }

        [Test]
        public void DeepCompareColumn_IdenticalComputedColumns_AreEqual()
        {
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(CreateComputedColumn(), CreateComputedColumn()), Is.True);
        }

        [Test]
        public void DeepCompareColumn_DefaultExpressionOnlyChange_IsDetected()
        {
            var source = CreateColumn(c => c.DefaultValue = "((0))");
            var target = CreateColumn(c => c.DefaultValue = "((1))");
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(source, target), Is.False);
        }

        [Test]
        public void DeepCompareColumn_AddDefault_IsDetected()
        {
            var source = CreateColumn();
            var target = CreateColumn(c => c.DefaultValue = "((0))");
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(source, target), Is.False);
        }

        [Test]
        public void DeepCompareColumn_RemoveDefault_IsDetected()
        {
            var source = CreateColumn(c => c.DefaultValue = "((0))");
            var target = CreateColumn();
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(source, target), Is.False);
        }

        [Test]
        public void DeepCompareColumn_ComputedFormulaOnlyChange_IsDetected()
        {
            var source = CreateComputedColumn();
            var target = CreateComputedColumn(c => c.ComputedFormula = "[Price] * [Quantity] * (1 + [TaxRate])");
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(source, target), Is.False);
        }

        [Test]
        public void DeepCompareColumn_PersistedFlagOnlyChange_IsDetected()
        {
            var source = CreateComputedColumn(c => c.ComputedPersisted = false);
            var target = CreateComputedColumn(c => c.ComputedPersisted = true);
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(source, target), Is.False);
        }

        [Test]
        public void DeepCompareColumn_PersistedFlagNullToTrue_IsDetected()
        {
            var source = CreateComputedColumn(c => c.ComputedPersisted = null);
            var target = CreateComputedColumn(c => c.ComputedPersisted = true);
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(source, target), Is.False);
        }

        [Test]
        public void DeepCompareColumn_ComputedToRegular_IsDetected()
        {
            var source = CreateComputedColumn();
            var target = CreateComputedColumn(c =>
            {
                c.IsComputed = false;
                c.ComputedFormula = null;
                c.ComputedPersisted = null;
                c.DataType = "money";
            });
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(source, target), Is.False);
        }

        [Test]
        public void DeepCompareColumn_RegularToComputed_IsDetected()
        {
            var source = CreateColumn();
            var target = CreateColumn(c =>
            {
                c.IsComputed = true;
                c.ComputedFormula = "[Price] * [Quantity]";
                c.ComputedPersisted = false;
            });
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(source, target), Is.False);
        }

        [Test]
        public void DeepCompareColumn_IdentitySeedOnlyChange_IsDetected()
        {
            var source = CreateColumn(c =>
            {
                c.IsIdentity = true;
                c.IdentitySeed = 1;
                c.IdentityIncrement = 1;
            });
            var target = CreateColumn(c =>
            {
                c.IsIdentity = true;
                c.IdentitySeed = 100;
                c.IdentityIncrement = 1;
            });
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(source, target), Is.False);
        }

        [Test]
        public void DeepCompareColumn_IdentityIncrementOnlyChange_IsDetected()
        {
            var source = CreateColumn(c =>
            {
                c.IsIdentity = true;
                c.IdentitySeed = 1;
                c.IdentityIncrement = 1;
            });
            var target = CreateColumn(c =>
            {
                c.IsIdentity = true;
                c.IdentitySeed = 1;
                c.IdentityIncrement = 5;
            });
            Assert.That(SchemaDesignerUtils.DeepCompareColumn(source, target), Is.False);
        }

        [Test]
        public void DeepCompareTable_DefaultOnlyColumnChange_MarksTableModified()
        {
            var source = CreateTable(CreateColumn());
            var target = CreateTable(CreateColumn(c => c.DefaultValue = "(getdate())"));
            Assert.That(SchemaDesignerUtils.DeepCompareTable(source, target), Is.False);
        }

        [Test]
        public void DeepCompareTable_ComputedFormulaOnlyColumnChange_MarksTableModified()
        {
            var source = CreateTable(CreateComputedColumn());
            var target = CreateTable(CreateComputedColumn(c => c.ComputedFormula = "[Price] + [Quantity]"));
            Assert.That(SchemaDesignerUtils.DeepCompareTable(source, target), Is.False);
        }

        [Test]
        public void DeepCompareTable_PersistedFlagOnlyColumnChange_MarksTableModified()
        {
            var source = CreateTable(CreateComputedColumn(c => c.ComputedPersisted = false));
            var target = CreateTable(CreateComputedColumn(c => c.ComputedPersisted = true));
            Assert.That(SchemaDesignerUtils.DeepCompareTable(source, target), Is.False);
        }

        [Test]
        public void DeepCompareTable_IdenticalTables_AreEqual()
        {
            var source = CreateTable(CreateComputedColumn(c => c.ComputedPersisted = true));
            var target = CreateTable(CreateComputedColumn(c => c.ComputedPersisted = true));
            Assert.That(SchemaDesignerUtils.DeepCompareTable(source, target), Is.True);
        }
    }
}
