//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using Dac = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using TableDesignerIssue = Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts.TableDesignerIssue;
using System.Linq;
namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    public static class TableDesignerValidator
    {
        private static List<ITableDesignerValidationRule> Rules = new List<ITableDesignerValidationRule>() {
            new IndexMustHaveColumnsRule(),
            new ForeignKeyMustHaveColumnsRule(),
            new ColumnCanOnlyAppearOnceInForeignKeyRule(),
            new ColumnCanOnlyAppearOnceInIndexRule(),
            new NoDuplicateColumnNameRule(),
            new NoDuplicateConstraintNameRule(),
            new NoDuplicateIndexNameRule(),
            new EdgeConstraintMustHaveClausesRule(),
            new EdgeConstraintNoRepeatingClausesRule(),
            new MemoryOptimizedCannotBeEnabledWhenNotSupportedRule(),
            new MemoryOptimizedTableMustHaveNonClusteredPrimaryKeyRule(),
            new TemporalTableMustHavePeriodColumnsRule(),
            new PeriodColumnsRule(),
            new ColumnsInPrimaryKeyCannotBeNullableRule(),
            new OnlyDurableMemoryOptimizedTableCanBeSystemVersionedRule(),
            new TemporalTableMustHavePrimaryKeyRule(),
            new TableMustHaveAtLeastOneColumnRule(),
            new MemoryOptimizedTableIdentityColumnRule(),
            new TableShouldAvoidHavingMultipleEdgeConstraintsRule(),
            new ColumnCannotBeListedMoreThanOnceInPrimaryKeyRule(),
            new MutipleCreateTableStatementsInScriptRule()
        };

        /// <summary>
        /// Validate the table and return the validation errors.
        /// </summary>
        public static List<TableDesignerIssue> Validate(Dac.TableDesigner designer)
        {
            var errors = new List<TableDesignerIssue>();
            foreach (var rule in Rules)
            {
                errors.AddRange(rule.Run(designer));
            }
            return errors;
        }
    }

    public interface ITableDesignerValidationRule
    {
        List<TableDesignerIssue> Run(Dac.TableDesigner designer);
    }

    public class IndexMustHaveColumnsRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            for (int i = 0; i < table.Indexes.Items.Count; i++)
            {
                var index = table.Indexes.Items[i];
                if (index.Columns.Count == 0)
                {
                    errors.Add(new TableDesignerIssue()
                    {
                        Description = SR.IndexMustHaveColumnsRuleDescription(index.Name),
                        PropertyPath = new object[] { TablePropertyNames.Indexes, i }
                    });
                }
            }
            return errors;
        }
    }

    public class ForeignKeyMustHaveColumnsRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            for (int i = 0; i < table.ForeignKeys.Items.Count; i++)
            {
                var foreignKey = table.ForeignKeys.Items[i];
                if (foreignKey.Columns.Count == 0)
                {
                    errors.Add(new TableDesignerIssue()
                    {
                        Description = SR.ForeignKeyMustHaveColumnsRuleDescription(foreignKey.Name),
                        PropertyPath = new object[] { TablePropertyNames.ForeignKeys, i }
                    });
                }
            }
            return errors;
        }
    }

    public class ColumnCanOnlyAppearOnceInIndexRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            for (int i = 0; i < table.Indexes.Items.Count; i++)
            {
                var index = table.Indexes.Items[i];
                var existingColumns = new HashSet<string>();
                for (int j = 0; j < index.Columns.Count; j++)
                {
                    var columnSpec = index.Columns[j];
                    if (existingColumns.Contains(columnSpec.Column))
                    {
                        errors.Add(new TableDesignerIssue()
                        {
                            Description = SR.ColumnCanOnlyAppearOnceInIndexRuleDescription(columnSpec.Column, index.Name, j + 1),
                            PropertyPath = new object[] { TablePropertyNames.Indexes, i, IndexPropertyNames.Columns, j }
                        });
                    }
                    else
                    {
                        existingColumns.Add(columnSpec.Column);
                    }
                }
            }
            return errors;
        }
    }

    public class ColumnCanOnlyAppearOnceInForeignKeyRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            for (int i = 0; i < table.ForeignKeys.Items.Count; i++)
            {
                var foreignKey = table.ForeignKeys.Items[i];
                var existingColumns = new HashSet<string>();
                for (int j = 0; j < foreignKey.Columns.Count; j++)
                {
                    var column = foreignKey.Columns[j];
                    if (existingColumns.Contains(column))
                    {
                        errors.Add(new TableDesignerIssue()
                        {
                            Description = SR.ColumnCanOnlyAppearOnceInForeignKeyRuleDescription(column, foreignKey.Name, j + 1),
                            PropertyPath = new object[] { TablePropertyNames.ForeignKeys, i, ForeignKeyPropertyNames.ColumnMapping, j, ForeignKeyColumnMappingPropertyNames.Column }
                        });
                    }
                    else
                    {
                        existingColumns.Add(column);
                    }
                }

                var existingForeignColumns = new HashSet<string>();
                for (int j = 0; j < foreignKey.ForeignColumns.Count; j++)
                {
                    var foreignColumn = foreignKey.ForeignColumns[j];
                    if (existingForeignColumns.Contains(foreignColumn))
                    {
                        errors.Add(new TableDesignerIssue()
                        {
                            Description = SR.ColumnCanOnlyAppearOnceInForeignKeyRuleForeignColumnDescription(foreignColumn, foreignKey.Name, j + 1),
                            PropertyPath = new object[] { TablePropertyNames.ForeignKeys, i, ForeignKeyPropertyNames.ColumnMapping, j, ForeignKeyColumnMappingPropertyNames.ForeignColumn }
                        });
                    }
                    else
                    {
                        existingForeignColumns.Add(foreignColumn);
                    }
                }
            }
            return errors;
        }
    }

    public class NoDuplicateConstraintNameRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var errors = new List<TableDesignerIssue>();
            var table = designer.TableViewModel;
            var existingNames = new HashSet<string>();
            for (int i = 0; i < table.ForeignKeys.Items.Count; i++)
            {
                var foreignKey = table.ForeignKeys.Items[i];
                if (existingNames.Contains(foreignKey.SystemName))
                {
                    errors.Add(new TableDesignerIssue()
                    {
                        Description = SR.NoDuplicateConstraintNameRuleDescription(foreignKey.SystemName, i + 1),
                        PropertyPath = new object[] { TablePropertyNames.ForeignKeys, i, ForeignKeyPropertyNames.Name }
                    });
                }
                else
                {
                    existingNames.Add(foreignKey.SystemName);
                }
            }

            for (int i = 0; i < table.CheckConstraints.Items.Count; i++)
            {
                var checkConstraint = table.CheckConstraints.Items[i];
                if (existingNames.Contains(checkConstraint.SystemName))
                {
                    errors.Add(new TableDesignerIssue()
                    {
                        Description = SR.NoDuplicateConstraintNameRuleDescription(checkConstraint.SystemName, i + 1),
                        PropertyPath = new object[] { TablePropertyNames.CheckConstraints, i, CheckConstraintPropertyNames.Name }
                    });
                }
                else
                {
                    existingNames.Add(checkConstraint.SystemName);
                }
            }

            for (int i = 0; i < table.EdgeConstraints.Items.Count; i++)
            {
                var edgeConstraint = table.EdgeConstraints.Items[i];
                if (existingNames.Contains(edgeConstraint.SystemName))
                {
                    errors.Add(new TableDesignerIssue()
                    {
                        Description = SR.NoDuplicateConstraintNameRuleDescription(edgeConstraint.SystemName, i + 1),
                        PropertyPath = new object[] { TablePropertyNames.EdgeConstraints, i, EdgeConstraintPropertyNames.Name }
                    });
                }
                else
                {
                    existingNames.Add(edgeConstraint.SystemName);
                }
            }
            return errors;
        }
    }

    public class NoDuplicateColumnNameRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            var existingNames = new HashSet<string>();
            for (int i = 0; i < table.Columns.Items.Count; i++)
            {
                var column = table.Columns.Items[i];
                if (existingNames.Contains(column.Name))
                {
                    errors.Add(new TableDesignerIssue()
                    {
                        Description = SR.NoDuplicateColumnNameRuleDescription(column.Name, i + 1),
                        PropertyPath = new object[] { TablePropertyNames.Columns, i, TableColumnPropertyNames.Name }
                    });
                }
                else
                {
                    existingNames.Add(column.Name);
                }
            }
            return errors;
        }
    }

    public class NoDuplicateIndexNameRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            var existingNames = new HashSet<string>();
            for (int i = 0; i < table.Indexes.Items.Count; i++)
            {
                var index = table.Indexes.Items[i];
                if (existingNames.Contains(index.Name))
                {
                    errors.Add(new TableDesignerIssue()
                    {
                        Description = SR.NoDuplicateIndexNameRuleDescription(index.Name, i + 1),
                        PropertyPath = new object[] { TablePropertyNames.Indexes, i, IndexPropertyNames.Name }
                    });
                }
                else
                {
                    existingNames.Add(index.Name);
                }
            }
            return errors;
        }
    }

    public class EdgeConstraintMustHaveClausesRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            for (int i = 0; i < table.EdgeConstraints.Items.Count; i++)
            {
                var edgeConstraint = table.EdgeConstraints.Items[i];
                if (edgeConstraint.Clauses.Count == 0)
                {
                    errors.Add(new TableDesignerIssue()
                    {
                        Description = SR.EdgeConstraintMustHaveClausesRuleDescription(edgeConstraint.Name),
                        PropertyPath = new object[] { TablePropertyNames.EdgeConstraints, i }
                    });
                }
            }
            return errors;
        }
    }

    public class EdgeConstraintNoRepeatingClausesRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            for (int i = 0; i < table.EdgeConstraints.Items.Count; i++)
            {
                var edgeConstraint = table.EdgeConstraints.Items[i];
                var existingPairs = new HashSet<string>();
                for (int j = 0; j < edgeConstraint.Clauses.Count; j++)
                {
                    var clause = edgeConstraint.Clauses[j];
                    var pair = string.Format("{0} - {1}", clause.FromTable, clause.ToTable);
                    if (existingPairs.Contains(pair))
                    {
                        errors.Add(new TableDesignerIssue()
                        {
                            Description = SR.EdgeConstraintNoRepeatingClausesRuleDescription(pair, j + 1),
                            PropertyPath = new object[] { TablePropertyNames.EdgeConstraints, i, EdgeConstraintPropertyNames.Clauses, j, EdgeConstraintClausePropertyNames.FromTable }
                        });
                    }
                    else
                    {
                        existingPairs.Add(pair);
                    }
                }
            }
            return errors;
        }
    }

    public class MemoryOptimizedTableMustHaveNonClusteredPrimaryKeyRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            if (table.IsMemoryOptimized && (table.PrimaryKey == null || table.PrimaryKey.IsClustered))
            {
                errors.Add(new TableDesignerIssue()
                {
                    Description = SR.MemoryOptimizedTableMustHaveNonClusteredPrimaryKeyRuleDescription,
                    PropertyPath = new object[] { TablePropertyNames.PrimaryKeyIsClustered }
                });
            }
            return errors;
        }
    }

    public class TemporalTableMustHavePrimaryKeyRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            if (table.SystemVersioningHistoryTable != null && table.PrimaryKey == null)
            {
                errors.Add(new TableDesignerIssue()
                {
                    Description = SR.TemporalTableMustHavePrimaryKeyRuleDescription
                });
            }
            return errors;
        }
    }

    public class TemporalTableMustHavePeriodColumnsRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            if (table.SystemVersioningHistoryTable != null && !table.PeriodColumnsDefined)
            {
                errors.Add(new TableDesignerIssue()
                {
                    Description = SR.TemporalTableMustHavePeriodColumnsRuleDescription,
                    MoreInfoLink = "https://docs.microsoft.com/sql/relational-databases/tables/creating-a-system-versioned-temporal-table"
                });
            }
            return errors;
        }
    }

    public class PeriodColumnsRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            var rowStart = table.Columns.Items.Where(c => c.GeneratedAlwaysAs == ColumnGeneratedAlwaysAsType.GeneratedAlwaysAsRowStart);
            var rowEnd = table.Columns.Items.Where(c => c.GeneratedAlwaysAs == ColumnGeneratedAlwaysAsType.GeneratedAlwaysAsRowEnd);
            if (rowStart.Count() > 1 || rowEnd.Count() > 1)
            {
                errors.Add(new TableDesignerIssue()
                {
                    Description = SR.PeriodColumnsRuleMoreThanOneIssueDescription
                });
            }
            else if (rowEnd.Count() != rowStart.Count())
            {
                errors.Add(new TableDesignerIssue()
                {
                    Description = SR.PeriodColumnsRuleNotMatchIssueDescription
                });
            }
            return errors;
        }
    }

    public class ColumnsInPrimaryKeyCannotBeNullableRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            for (int i = 0; i < table.Columns.Items.Count; i++)
            {
                var column = table.Columns.Items[i];
                if (column.IsPrimaryKey && column.IsNullable)
                {
                    errors.Add(new TableDesignerIssue()
                    {
                        Description = SR.ColumnsInPrimaryKeyCannotBeNullableRuleDescription,
                        PropertyPath = new object[] { TablePropertyNames.Columns, i }
                    });
                }
            }
            return errors;
        }
    }

    public class OnlyDurableMemoryOptimizedTableCanBeSystemVersionedRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            if (table.Durability == TableDurability.SchemaOnly && table.IsMemoryOptimized && table.IsSystemVersioningEnabled)
            {
                errors.Add(new TableDesignerIssue()
                {
                    Description = SR.OnlyDurableMemoryOptimizedTableCanBeSystemVersionedRuleDescription
                });
            }
            return errors;
        }
    }

    public class TableMustHaveAtLeastOneColumnRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            if (!table.IsEdge && table.Columns.Items.Where(c => !c.IsComputed).Count() == 0)
            {
                errors.Add(new TableDesignerIssue()
                {
                    Description = SR.TableMustHaveAtLeastOneColumnRuleDescription
                });
            }
            return errors;
        }
    }

    public class MemoryOptimizedTableIdentityColumnRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            if (table.IsMemoryOptimized)
            {
                for (int i = 0; i < table.Columns.Items.Count; i++)
                {
                    var column = table.Columns.Items[i];
                    if (column.IsIdentity && (column.IdentitySeed != 1 || column.IdentityIncrement != 1))
                    {
                        var propertyName = column.IdentitySeed != 1 ? TableColumnPropertyNames.IdentitySeed : TableColumnPropertyNames.IdentityIncrement;
                        errors.Add(new TableDesignerIssue()
                        {
                            Description = SR.MemoryOptimizedTableIdentityColumnRuleDescription,
                            PropertyPath = new object[] { TablePropertyNames.Columns, i, propertyName }
                        });
                    }
                }
            }
            return errors;
        }
    }

    public class TableShouldAvoidHavingMultipleEdgeConstraintsRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            if (table.EdgeConstraints.Items.Count > 1)
            {
                errors.Add(new TableDesignerIssue()
                {
                    Description = SR.TableShouldAvoidHavingMultipleEdgeConstraintsRuleDescription,
                    Severity = Contracts.IssueSeverity.Warning,
                    MoreInfoLink = "https://docs.microsoft.com/sql/relational-databases/tables/graph-edge-constraints"
                });
            }
            return errors;
        }
    }

    public class ColumnCannotBeListedMoreThanOnceInPrimaryKeyRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            if (table.PrimaryKey != null)
            {
                var existingNames = new HashSet<string>();
                for (int i = 0; i < table.PrimaryKey.Columns.Count; i++)
                {
                    var columnSpec = table.PrimaryKey.Columns[i];
                    if (existingNames.Contains(columnSpec.Column))
                    {
                        errors.Add(new TableDesignerIssue()
                        {
                            Description = SR.ColumnCannotBeListedMoreThanOnceInPrimaryKeyRuleDescription(columnSpec.Column),
                            PropertyPath = new object[] { TablePropertyNames.PrimaryKeyColumns, i, IndexColumnSpecificationPropertyNames.Column }
                        });
                    }
                    else
                    {
                        existingNames.Add(columnSpec.Column);
                    }
                }
            }
            return errors;
        }
    }

    public class MemoryOptimizedCannotBeEnabledWhenNotSupportedRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var table = designer.TableViewModel;
            var errors = new List<TableDesignerIssue>();
            if (!designer.IsMemoryOptimizedTableSupported && designer.TableViewModel.IsMemoryOptimized)
            {
                errors.Add(new TableDesignerIssue()
                {
                    Description = SR.MemoryOptimizedCannotBeEnabledWhenNotSupportedRuleDescription,
                    PropertyPath = new object[] { TablePropertyNames.IsMemoryOptimized },
                    MoreInfoLink = designer.IsAzure
                        ? "https://docs.microsoft.com/en-us/azure/azure-sql/in-memory-oltp-overview"
                        : "https://docs.microsoft.com/en-us/sql/relational-databases/in-memory-oltp/overview-and-usage-scenarios"
                });
            }
            return errors;
        }
    }

    public class MutipleCreateTableStatementsInScriptRule : ITableDesignerValidationRule
    {
        public List<TableDesignerIssue> Run(Dac.TableDesigner designer)
        {
            var errors = new List<TableDesignerIssue>();
            if (designer.ScriptContainsMultipleTableDefinition)
            {
                errors.Add(new TableDesignerIssue()
                {
                    Description = SR.MutipleCreateTableStatementsInScriptRuleDescription,
                    Severity = Contracts.IssueSeverity.Information
                });
            }
            return errors;
        }
    }
}