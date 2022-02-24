//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using ValidationError = Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts.TableDesignerValidationError;
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
            new NoDuplicateIndexNameRule()
        };

        /// <summary>
        /// Validate the table and return the validation errors.
        /// </summary>
        public static List<ValidationError> Validate(TableViewModel table)
        {
            var errors = new List<ValidationError>();
            foreach (var rule in Rules)
            {
                errors.AddRange(rule.Run(table));
            }
            return errors;
        }
    }

    public interface ITableDesignerValidationRule
    {
        List<ValidationError> Run(TableViewModel table);
    }

    public class IndexMustHaveColumnsRule : ITableDesignerValidationRule
    {
        public List<ValidationError> Run(TableViewModel table)
        {
            var errors = new List<ValidationError>();
            for (int i = 0; i < table.Indexes.Items.Count; i++)
            {
                var index = table.Indexes.Items[i];
                if (index.Columns.Count == 0)
                {
                    errors.Add(new ValidationError()
                    {
                        Message = string.Format("Index '{0}' does not have any columns associated with it.", index.Name),
                        PropertyPath = new object[] { TablePropertyNames.Indexes, i }
                    });
                }
            }
            return errors;
        }
    }

    public class ForeignKeyMustHaveColumnsRule : ITableDesignerValidationRule
    {
        public List<ValidationError> Run(TableViewModel table)
        {
            var errors = new List<ValidationError>();
            for (int i = 0; i < table.ForeignKeys.Items.Count; i++)
            {
                var foreignKey = table.ForeignKeys.Items[i];
                if (foreignKey.Columns.Count == 0)
                {
                    errors.Add(new ValidationError()
                    {
                        Message = string.Format("Foreign key '{0}' does not have any column mapping specified.", foreignKey.Name),
                        PropertyPath = new object[] { TablePropertyNames.ForeignKeys, i }
                    });
                }
            }
            return errors;
        }
    }

    public class ColumnCanOnlyAppearOnceInIndexRule : ITableDesignerValidationRule
    {
        public List<ValidationError> Run(TableViewModel table)
        {
            var errors = new List<ValidationError>();
            for (int i = 0; i < table.Indexes.Items.Count; i++)
            {
                var index = table.Indexes.Items[i];
                var existingColumns = new HashSet<string>();
                for (int j = 0; j < index.Columns.Count; j++)
                {
                    var columnSpec = index.Columns[j];
                    if (existingColumns.Contains(columnSpec.Column))
                    {
                        errors.Add(new ValidationError()
                        {
                            Message = string.Format("Column with name '{0}' has already been added to the index '{1}'. Row number: {2}.", columnSpec.Column, index.Name, j + 1),
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
        public List<ValidationError> Run(TableViewModel table)
        {
            var errors = new List<ValidationError>();
            for (int i = 0; i < table.ForeignKeys.Items.Count; i++)
            {
                var foreignKey = table.ForeignKeys.Items[i];
                var existingColumns = new HashSet<string>();
                for (int j = 0; j < foreignKey.Columns.Count; j++)
                {
                    var column = foreignKey.Columns[j];
                    if (existingColumns.Contains(column))
                    {
                        errors.Add(new ValidationError()
                        {
                            Message = string.Format("Column with name '{0}' has already been added to the foreign key '{1}'. Row number: {2}.", column, foreignKey.Name, j + 1),
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
                        errors.Add(new ValidationError()
                        {
                            Message = string.Format("Foreign column with name '{0}' has already been added to the foreign key '{1}'. Row number: {2}.", foreignColumn, foreignKey.Name, j + 1),
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
        public List<ValidationError> Run(TableViewModel table)
        {
            var errors = new List<ValidationError>();
            var existingNames = new HashSet<string>();
            for (int i = 0; i < table.ForeignKeys.Items.Count; i++)
            {
                var foreignKey = table.ForeignKeys.Items[i];
                if (existingNames.Contains(foreignKey.Name))
                {
                    errors.Add(new ValidationError()
                    {
                        Message = string.Format("The name '{0}' is already used by another constraint. Row number: {1}.", foreignKey.Name, i + 1),
                        PropertyPath = new object[] { TablePropertyNames.ForeignKeys, i, ForeignKeyPropertyNames.Name }
                    });
                }
                else
                {
                    existingNames.Add(foreignKey.Name);
                }
            }

            for (int i = 0; i < table.CheckConstraints.Items.Count; i++)
            {
                var checkConstraint = table.CheckConstraints.Items[i];
                if (existingNames.Contains(checkConstraint.Name))
                {
                    errors.Add(new ValidationError()
                    {
                        Message = string.Format("The name '{0}' is already used by another constraint. Row number: {1}.", checkConstraint.Name, i + 1),
                        PropertyPath = new object[] { TablePropertyNames.CheckConstraints, i, CheckConstraintPropertyNames.Name }
                    });
                }
                else
                {
                    existingNames.Add(checkConstraint.Name);
                }
            }
            return errors;
        }
    }

    public class NoDuplicateColumnNameRule : ITableDesignerValidationRule
    {
        public List<ValidationError> Run(TableViewModel table)
        {
            var errors = new List<ValidationError>();
            var existingNames = new HashSet<string>();
            for (int i = 0; i < table.Columns.Items.Count; i++)
            {
                var column = table.Columns.Items[i];
                if (existingNames.Contains(column.Name))
                {
                    errors.Add(new ValidationError()
                    {
                        Message = string.Format("The name '{0}' is already used by another column. Row number: {1}.", column.Name, i + 1),
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
        public List<ValidationError> Run(TableViewModel table)
        {
            var errors = new List<ValidationError>();
            var existingNames = new HashSet<string>();
            for (int i = 0; i < table.Indexes.Items.Count; i++)
            {
                var index = table.Indexes.Items[i];
                if (existingNames.Contains(index.Name))
                {
                    errors.Add(new ValidationError()
                    {
                        Message = string.Format("The name '{0}' is already used by another index. Row number: {1}.", index.Name, i + 1),
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
}