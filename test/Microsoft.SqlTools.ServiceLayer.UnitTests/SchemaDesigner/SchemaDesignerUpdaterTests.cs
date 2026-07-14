//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlTools.ServiceLayer.SchemaDesigner;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SchemaDesigner
{
    /// <summary>
    /// Tests for the pure decision helpers in <see cref="SchemaDesignerUpdater"/> that
    /// determine whether computed column properties must be propagated when both the
    /// source and target columns remain computed. Applying the resulting updates to the
    /// DacFx <c>TableColumnViewModel</c> requires a live designer session and is covered
    /// by the integration lane.
    /// </summary>
    public class SchemaDesignerUpdaterTests
    {
        private static readonly Guid ColumnId = new Guid("33333333-3333-3333-3333-333333333333");

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

        [Test]
        public void ShouldUpdateComputedFormula_FormulaOnlyChange_ReturnsTrue()
        {
            var source = CreateComputedColumn();
            var target = CreateComputedColumn(c => c.ComputedFormula = "[Price] * [Quantity] * (1 + [TaxRate])");
            Assert.That(SchemaDesignerUpdater.ShouldUpdateComputedFormula(source, target), Is.True);
        }

        [Test]
        public void ShouldUpdateComputedFormula_UnchangedFormula_ReturnsFalse()
        {
            Assert.That(SchemaDesignerUpdater.ShouldUpdateComputedFormula(CreateComputedColumn(), CreateComputedColumn()), Is.False);
        }

        [Test]
        public void ShouldUpdateComputedFormula_NotBothComputed_ReturnsFalse()
        {
            var source = CreateComputedColumn(c =>
            {
                c.IsComputed = false;
                c.ComputedFormula = null;
            });
            var target = CreateComputedColumn();
            // The IsComputed transition itself is handled by the dedicated updater branch;
            // the both-computed helper must not claim it.
            Assert.That(SchemaDesignerUpdater.ShouldUpdateComputedFormula(source, target), Is.False);
        }

        [Test]
        public void ShouldUpdateComputedPersisted_PersistedFlagOnlyChange_ReturnsTrue()
        {
            var source = CreateComputedColumn(c => c.ComputedPersisted = false);
            var target = CreateComputedColumn(c => c.ComputedPersisted = true);
            Assert.That(SchemaDesignerUpdater.ShouldUpdateComputedPersisted(source, target), Is.True);
        }

        [Test]
        public void ShouldUpdateComputedPersisted_NullToTrue_ReturnsTrue()
        {
            var source = CreateComputedColumn(c => c.ComputedPersisted = null);
            var target = CreateComputedColumn(c => c.ComputedPersisted = true);
            Assert.That(SchemaDesignerUpdater.ShouldUpdateComputedPersisted(source, target), Is.True);
        }

        [Test]
        public void ShouldUpdateComputedPersisted_Unchanged_ReturnsFalse()
        {
            Assert.That(SchemaDesignerUpdater.ShouldUpdateComputedPersisted(CreateComputedColumn(), CreateComputedColumn()), Is.False);
        }

        [Test]
        public void ShouldUpdateComputedPersistedNullable_NullabilityOnlyChange_ReturnsTrue()
        {
            var source = CreateComputedColumn(c => c.IsNullable = true);
            var target = CreateComputedColumn(c => c.IsNullable = false);
            Assert.That(SchemaDesignerUpdater.ShouldUpdateComputedPersistedNullable(source, target), Is.True);
        }

        [Test]
        public void ShouldUpdateComputedPersistedNullable_Unchanged_ReturnsFalse()
        {
            Assert.That(SchemaDesignerUpdater.ShouldUpdateComputedPersistedNullable(CreateComputedColumn(), CreateComputedColumn()), Is.False);
        }
    }
}
