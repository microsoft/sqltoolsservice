//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.SqlCore.SchemaCompare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using CoreOps = Microsoft.SqlTools.SqlCore.SchemaCompare;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    /// <summary>
    /// Focused Fabric Warehouse (SqlDwUnified) Schema Compare tests that verify the
    /// reflection-free platform surfacing and the constraint ALTER-script carve-out end to
    /// end, using dacpacs built entirely offline (no live Fabric Warehouse required).
    ///
    /// These tests depend on the DacFx version-mapping fix that makes
    /// <c>TSqlModel.Version</c> report <c>SqlDwUnified</c> for Fabric Warehouse models
    /// (previously it returned <c>Sql150</c>, which forced a reflection workaround in STS).
    /// </summary>
    public class SchemaCompareFabricPlatformTests
    {
        // Fabric Warehouse requires NONCLUSTERED / NOT ENFORCED constraints.
        private const string FabricSourceScript = @"
CREATE TABLE [dbo].[Orders] (
    [OrderID] INT          NOT NULL,
    [Sku]     VARCHAR (20) NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY NONCLUSTERED ([OrderID]) NOT ENFORCED,
    CONSTRAINT [AK_Orders_Sku] UNIQUE NONCLUSTERED ([Sku]) NOT ENFORCED
);
GO";

        private const string FabricTargetScript = @"
CREATE TABLE [dbo].[Orders] (
    [OrderID] INT          NOT NULL,
    [Sku]     VARCHAR (20) NOT NULL
);
GO";

        /// <summary>
        /// Verifies that a Fabric Warehouse (SqlDwUnified) dacpac-to-dacpac comparison:
        ///   1. surfaces SourcePlatform/TargetPlatform as "SqlDwUnified" read directly from the
        ///      public TSqlModel.Version (no reflection), and
        ///   2. preserves the standalone "ALTER TABLE ... ADD CONSTRAINT" script for the table's
        ///      constraints instead of stripping it (the SqlDwUnified carve-out in CreateDiffEntry).
        /// </summary>
        [Test]
        public void FabricWarehouse_SurfacesPlatformAndPreservesConstraintAlterScript()
        {
            string workingFolder = Path.Combine(Path.GetTempPath(), "SchemaCompareFabricPlatformTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingFolder);
            string sourceDacpac = Path.Combine(workingFolder, "source.dacpac");
            string targetDacpac = Path.Combine(workingFolder, "target.dacpac");

            try
            {
                BuildFabricDacpac(FabricSourceScript, sourceDacpac);
                BuildFabricDacpac(FabricTargetScript, targetDacpac);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo
                {
                    EndpointType = SchemaCompareEndpointType.Dacpac,
                    PackageFilePath = sourceDacpac
                };
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo
                {
                    EndpointType = SchemaCompareEndpointType.Dacpac,
                    PackageFilePath = targetDacpac
                };

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                // dacpac-to-dacpac needs no live connection.
                SchemaCompareOperation operation = new CoreOps.SchemaCompareOperation(schemaCompareParams, new TestConnectionProvider(null, null));
                operation.Execute();

                Assert.IsNull(operation.ErrorMessage, "Fabric dacpac comparison should not error. " + operation.ErrorMessage);
                Assert.IsTrue(operation.ComparisonResult.IsValid, "Fabric dacpac comparison should be valid.");
                Assert.IsFalse(operation.ComparisonResult.IsEqual, "Source adds constraints the target lacks; expected differences.");

                // (1) Reflection-free platform surfacing: read from public TSqlModel.Version.
                Assert.AreEqual("SqlDwUnified", operation.SourcePlatform,
                    "SourcePlatform should be SqlDwUnified (read from ComparisonResult.SourceModel.Version).");
                Assert.AreEqual("SqlDwUnified", operation.TargetPlatform,
                    "TargetPlatform should be SqlDwUnified (read from ComparisonResult.TargetModel.Version).");

                // (2) Constraint carve-out: the primary key surfaces as a diff whose ADD CONSTRAINT
                // script is preserved rather than stripped by the strip-on-alter heuristic.
                DiffEntry pkConstraint = FindDiff(operation.Differences,
                    d => (d.SourceObjectType != null && d.SourceObjectType.EndsWith("PrimaryKeyConstraint", StringComparison.Ordinal)));
                Assert.IsNotNull(pkConstraint, "Expected a diff entry for the PRIMARY KEY constraint on Fabric.");
                Assert.IsFalse(string.IsNullOrEmpty(pkConstraint.SourceScript),
                    "Fabric constraint diff must keep its standalone ALTER script (carve-out), not strip it.");
                StringAssert.Contains("ADD CONSTRAINT", pkConstraint.SourceScript,
                    "Preserved Fabric constraint script should be the standalone ALTER TABLE ... ADD CONSTRAINT.");
            }
            finally
            {
                try { Directory.Delete(workingFolder, recursive: true); } catch { /* best-effort cleanup */ }
            }
        }

        private static DiffEntry FindDiff(IEnumerable<DiffEntry> entries, Predicate<DiffEntry> match)
        {
            if (entries == null)
            {
                return null;
            }
            foreach (DiffEntry entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }
                if (match(entry))
                {
                    return entry;
                }
                DiffEntry childMatch = FindDiff(entry.Children, match);
                if (childMatch != null)
                {
                    return childMatch;
                }
            }
            return null;
        }

        /// <summary>
        /// Builds a Fabric Warehouse (SqlDwUnified) dacpac entirely offline from a T-SQL script.
        /// </summary>
        internal static string BuildFabricDacpac(string sqlScript, string dacpacPath)
        {
            using (TSqlModel model = new TSqlModel(SqlServerVersion.SqlDwUnified, new TSqlModelOptions()))
            {
                foreach (string batch in SplitOnGo(sqlScript))
                {
                    if (string.IsNullOrWhiteSpace(batch))
                    {
                        continue;
                    }
                    model.AddObjects(batch);
                }

                DacPackageExtensions.BuildPackage(
                    dacpacPath,
                    model,
                    new PackageMetadata { Name = "FabricTestPackage", Version = "1.0.0.0", Description = "Fabric Warehouse Schema Compare test fixture" });
            }
            return dacpacPath;
        }

        private static IEnumerable<string> SplitOnGo(string sqlScript)
        {
            if (string.IsNullOrEmpty(sqlScript))
            {
                yield break;
            }

            // Split on lines that are exactly "GO" (case-insensitive), matching how DacFx
            // consumes multi-batch .sql files.
            var current = new System.Text.StringBuilder();
            foreach (string line in sqlScript.Replace("\r\n", "\n").Split('\n'))
            {
                if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    yield return current.ToString();
                    current.Clear();
                }
                else
                {
                    current.AppendLine(line);
                }
            }
            if (current.Length > 0)
            {
                yield return current.ToString();
            }
        }
    }
}
