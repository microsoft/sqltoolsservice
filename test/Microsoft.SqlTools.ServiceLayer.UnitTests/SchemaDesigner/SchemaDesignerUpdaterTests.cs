//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.SchemaDesigner;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SchemaDesigner
{
    public class SchemaDesignerUpdaterTests
    {
        #region TrackTableChange Tests
        
        [Test]
        public void TrackTableChange_NewEntry_AddsToReportDictionary()
        {
            // Arrange
            var reportDictionary = new Dictionary<string, SchemaDesignerReportObject>();
            var table = new SchemaDesignerTable
            {
                Id = Guid.NewGuid(),
                Schema = "dbo",
                Name = "TestTable"
            };
            var tableState = SchemaDesignerReportTableState.CREATED;
            var sqlScript = "CREATE TABLE [dbo].[TestTable] (Id INT PRIMARY KEY);";
            var changeDescription = "Creating new table 'dbo.TestTable'";

            // Act
            SchemaDesignerUpdater.TrackTableChange(reportDictionary, table, tableState, sqlScript, changeDescription);

            // Assert
            Assert.That(reportDictionary.Count, Is.EqualTo(1));
            Assert.That(reportDictionary.ContainsKey(table.Id.ToString()), Is.True);
            Assert.That(reportDictionary[table.Id.ToString()].TableState, Is.EqualTo(tableState));
            Assert.That(reportDictionary[table.Id.ToString()].UpdateScript, Is.EqualTo(sqlScript));
            Assert.That(reportDictionary[table.Id.ToString()].ActionsPerformed, Has.Count.EqualTo(1));
            Assert.That(reportDictionary[table.Id.ToString()].ActionsPerformed[0], Is.EqualTo(changeDescription));
        }

        [Test]
        public void TrackTableChange_ExistingEntry_UpdatesReportDictionary()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var reportDictionary = new Dictionary<string, SchemaDesignerReportObject>
            {
                {
                    tableId.ToString(), new SchemaDesignerReportObject
                    {
                        TableId = tableId,
                        TableName = "dbo.TestTable",
                        UpdateScript = "CREATE TABLE [dbo].[TestTable] (Id INT PRIMARY KEY);",
                        TableState = SchemaDesignerReportTableState.CREATED,
                        ActionsPerformed = new List<string> { "Creating new table 'dbo.TestTable'" }
                    }
                }
            };

            var table = new SchemaDesignerTable
            {
                Id = tableId,
                Schema = "dbo",
                Name = "TestTable"
            };
            var tableState = SchemaDesignerReportTableState.UPDATED;
            var sqlScript = "ALTER TABLE [dbo].[TestTable] ADD Name NVARCHAR(50);";
            var changeDescription = "Adding column 'Name'";

            // Act
            SchemaDesignerUpdater.TrackTableChange(reportDictionary, table, tableState, sqlScript, changeDescription);

            // Assert
            Assert.That(reportDictionary.Count, Is.EqualTo(1));
            Assert.That(reportDictionary[tableId.ToString()].TableState, Is.EqualTo(tableState));
            Assert.That(reportDictionary[tableId.ToString()].UpdateScript, Is.EqualTo("CREATE TABLE [dbo].[TestTable] (Id INT PRIMARY KEY);ALTER TABLE [dbo].[TestTable] ADD Name NVARCHAR(50);"));
            Assert.That(reportDictionary[tableId.ToString()].ActionsPerformed, Has.Count.EqualTo(2));
            Assert.That(reportDictionary[tableId.ToString()].ActionsPerformed[1], Is.EqualTo(changeDescription));
        }

        [Test]
        public void TrackTableChange_TableStateDowngrade_MaintainsHigherState()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var reportDictionary = new Dictionary<string, SchemaDesignerReportObject>
            {
                {
                    tableId.ToString(), new SchemaDesignerReportObject
                    {
                        TableId = tableId,
                        TableName = "dbo.TestTable",
                        UpdateScript = "CREATE TABLE [dbo].[TestTable] (Id INT PRIMARY KEY);",
                        TableState = SchemaDesignerReportTableState.DROPPED,
                        ActionsPerformed = new List<string> { "Dropping table 'dbo.TestTable'" }
                    }
                }
            };

            var table = new SchemaDesignerTable
            {
                Id = tableId,
                Schema = "dbo",
                Name = "TestTable"
            };
            var tableState = SchemaDesignerReportTableState.UPDATED; // Lower than DROPPED
            var sqlScript = "ALTER TABLE [dbo].[TestTable] ADD Name NVARCHAR(50);";
            var changeDescription = "Adding column 'Name'";

            // Act
            SchemaDesignerUpdater.TrackTableChange(reportDictionary, table, tableState, sqlScript, changeDescription);

            // Assert
            Assert.That(reportDictionary[tableId.ToString()].TableState, Is.EqualTo(SchemaDesignerReportTableState.DROPPED));
        }

        #endregion
        #region GenerateUpdateScripts Tests

        [Test]
        public void GenerateUpdateScripts_NullSourceSchema_ThrowsArgumentNullException()
        {
            // Arrange
            SchemaDesignerModel? sourceSchema = null;
            var targetSchema = new SchemaDesignerModel { Tables = new List<SchemaDesignerTable>() };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SchemaDesignerUpdater.GenerateUpdateScripts(sourceSchema, targetSchema));
        }

        [Test]
        public void GenerateUpdateScripts_NullTargetSchema_ThrowsArgumentNullException()
        {
            // Arrange
            var sourceSchema = new SchemaDesignerModel { Tables = new List<SchemaDesignerTable>() };
            SchemaDesignerModel? targetSchema = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SchemaDesignerUpdater.GenerateUpdateScripts(sourceSchema, targetSchema));
        }

        [Test]
        public void GenerateUpdateScripts_NoChanges_ReturnsEmptyScript()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var columnId = Guid.NewGuid();

            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = columnId,
                                Name = "Id",
                                DataType = "INT",
                                IsPrimaryKey = true,
                                IsNullable = false
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = columnId,
                                Name = "Id",
                                DataType = "INT",
                                IsPrimaryKey = true,
                                IsNullable = false
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            // Mock the DeepCompareTable and DeepCompareColumn to return true (no changes)
            // Note: In a real implementation, you might need to use a mocking framework like Moq

            // Act
            var result = SchemaDesignerUpdater.GenerateUpdateScripts(sourceSchema, targetSchema);

            // Assert
            Assert.That(string.IsNullOrWhiteSpace(result.UpdateScript), Is.True);
            Assert.That(result.Reports, Is.Empty);
        }

        [Test]
        public void GenerateUpdateScripts_AddNewTable_GeneratesCreateTableScript()
        {
            // Arrange
            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>()
            };

            var targetTableId = Guid.NewGuid();
            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = targetTableId,
                        Schema = "dbo",
                        Name = "NewTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = Guid.NewGuid(),
                                Name = "Id",
                                DataType = "INT",
                                IsPrimaryKey = true,
                                IsNullable = false
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            // Act
            var result = SchemaDesignerUpdater.GenerateUpdateScripts(sourceSchema, targetSchema);

            // Assert
            Assert.That(result.Reports.Count, Is.EqualTo(1));
            Assert.That(result.Reports[0].TableId, Is.EqualTo(targetTableId));
            Assert.That(result.Reports[0].TableState, Is.EqualTo(SchemaDesignerReportTableState.CREATED));
            Assert.That(result.Reports[0].ActionsPerformed[0], Does.Contain("Creating new table"));
            Assert.That(result.UpdateScript, Does.Contain("CREATE TABLE"));
        }

        [Test]
        public void GenerateUpdateScripts_DropTable_GeneratesDropTableScript()
        {
            // Arrange
            var sourceTableId = Guid.NewGuid();
            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = sourceTableId,
                        Schema = "dbo",
                        Name = "OldTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = Guid.NewGuid(),
                                Name = "Id",
                                DataType = "INT",
                                IsPrimaryKey = true,
                                IsNullable = false
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>()
            };

            // Act
            var result = SchemaDesignerUpdater.GenerateUpdateScripts(sourceSchema, targetSchema);

            // Assert
            Assert.That(result.Reports.Count, Is.EqualTo(1));
            Assert.That(result.Reports[0].TableId, Is.EqualTo(sourceTableId));
            Assert.That(result.Reports[0].TableState, Is.EqualTo(SchemaDesignerReportTableState.DROPPED));
            Assert.That(result.Reports[0].ActionsPerformed[0], Does.Contain("Dropping table"));
            Assert.That(result.UpdateScript, Does.Contain("DROP TABLE"));
        }

        [Test]
        public void GenerateUpdateScripts_ModifyColumnDataType_GeneratesAlterColumnScript()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var columnId = Guid.NewGuid();

            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = columnId,
                                Name = "Name",
                                DataType = "VARCHAR",
                                MaxLength = 50,
                                IsNullable = true
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = columnId,
                                Name = "Name",
                                DataType = "NVARCHAR",
                                MaxLength = 100,
                                IsNullable = true
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            // Act
            var result = SchemaDesignerUpdater.GenerateUpdateScripts(sourceSchema, targetSchema);

            // Assert
            Assert.That(result.Reports.Count, Is.EqualTo(1));
            Assert.That(result.Reports[0].TableId, Is.EqualTo(tableId));
            Assert.That(result.Reports[0].TableState, Is.EqualTo(SchemaDesignerReportTableState.UPDATED));
            Assert.That(result.Reports[0].ActionsPerformed, Has.Some.Contains("Modifying column"));
            Assert.That(result.UpdateScript, Does.Contain("ALTER COLUMN"));
        }

        [Test]
        public void GenerateUpdateScripts_AddColumn_GeneratesAddColumnScript()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var existingColumnId = Guid.NewGuid();
            var newColumnId = Guid.NewGuid();

            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = existingColumnId,
                                Name = "Id",
                                DataType = "INT",
                                IsPrimaryKey = true,
                                IsNullable = false
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = existingColumnId,
                                Name = "Id",
                                DataType = "INT",
                                IsPrimaryKey = true,
                                IsNullable = false
                            },
                            new SchemaDesignerColumn
                            {
                                Id = newColumnId,
                                Name = "Description",
                                DataType = "NVARCHAR",
                                MaxLength = 255,
                                IsNullable = true
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            // Act
            var result = SchemaDesignerUpdater.GenerateUpdateScripts(sourceSchema, targetSchema);

            // Assert
            Assert.That(result.Reports.Count, Is.EqualTo(1));
            Assert.That(result.Reports[0].TableId, Is.EqualTo(tableId));
            Assert.That(result.Reports[0].TableState, Is.EqualTo(SchemaDesignerReportTableState.UPDATED));
            Assert.That(result.Reports[0].ActionsPerformed, Has.Some.Contains("Adding new column"));
            Assert.That(result.UpdateScript, Does.Contain("ADD"));
        }

        [Test]
        public void GenerateUpdateScripts_DropColumn_GeneratesDropColumnScript()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var remainingColumnId = Guid.NewGuid();
            var droppedColumnId = Guid.NewGuid();

            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = remainingColumnId,
                                Name = "Id",
                                DataType = "INT",
                                IsPrimaryKey = true,
                                IsNullable = false
                            },
                            new SchemaDesignerColumn
                            {
                                Id = droppedColumnId,
                                Name = "OldColumn",
                                DataType = "VARCHAR",
                                MaxLength = 50,
                                IsNullable = true
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = remainingColumnId,
                                Name = "Id",
                                DataType = "INT",
                                IsPrimaryKey = true,
                                IsNullable = false
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            // Act
            var result = SchemaDesignerUpdater.GenerateUpdateScripts(sourceSchema, targetSchema);

            // Assert
            Assert.That(result.Reports.Count, Is.EqualTo(1));
            Assert.That(result.Reports[0].TableId, Is.EqualTo(tableId));
            Assert.That(result.Reports[0].TableState, Is.EqualTo(SchemaDesignerReportTableState.UPDATED));
            Assert.That(result.Reports[0].ActionsPerformed, Has.Some.Contains("Dropping column"));
            Assert.That(result.UpdateScript, Does.Contain("DROP COLUMN"));
        }

        [Test]
        public void GenerateUpdateScripts_AddForeignKey_GeneratesAddForeignKeyScript()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var refTableId = Guid.NewGuid();
            var columnId = Guid.NewGuid();
            var refColumnId = Guid.NewGuid();

            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = columnId,
                                Name = "CategoryId",
                                DataType = "INT",
                                IsNullable = false
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    },
                    new SchemaDesignerTable
                    {
                        Id = refTableId,
                        Schema = "dbo",
                        Name = "Categories",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = refColumnId,
                                Name = "Id",
                                DataType = "INT",
                                IsPrimaryKey = true,
                                IsNullable = false
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = columnId,
                                Name = "CategoryId",
                                DataType = "INT",
                                IsNullable = false
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>
                        {
                            new SchemaDesignerForeignKey
                            {
                                Name = "FK_TestTable_Categories",
                                Columns = new List<string> { "CategoryId" },
                                ReferencedSchemaName = "dbo",
                                ReferencedTableName = "Categories",
                                ReferencedColumns = new List<string> { "Id" },
                                OnDeleteAction = OnAction.NO_ACTION,
                                OnUpdateAction = OnAction.NO_ACTION,
                            }
                        }
                    },
                    new SchemaDesignerTable
                    {
                        Id = refTableId,
                        Schema = "dbo",
                        Name = "Categories",
                        Columns = new List<SchemaDesignerColumn>
                        {
                            new SchemaDesignerColumn
                            {
                                Id = refColumnId,
                                Name = "Id",
                                DataType = "INT",
                                IsPrimaryKey = true,
                                IsNullable = false
                            }
                        },
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            // Act
            var result = SchemaDesignerUpdater.GenerateUpdateScripts(sourceSchema, targetSchema);

            // Assert
            Assert.That(result.Reports.Count, Is.EqualTo(1));
            Assert.That(result.Reports[0].TableId, Is.EqualTo(tableId));
            Assert.That(result.Reports[0].TableState, Is.EqualTo(SchemaDesignerReportTableState.UPDATED));
            Assert.That(result.Reports[0].ActionsPerformed, Has.Some.Contains("Adding new foreign key"));
            Assert.That(result.UpdateScript, Does.Contain("FOREIGN KEY"));
            Assert.That(result.UpdateScript, Does.Contain("REFERENCES"));
        }

        #endregion

        #region ColumnChangeDescription tests

        [Test]
        public void GetColumnChangeDescription_DataTypeChange_ReportsTypeChanged()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsNullable = true
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "NVARCHAR",
                MaxLength = 50,
                IsNullable = true
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("type changed from 'VARCHAR' to 'NVARCHAR'"));
            Assert.That(result, Does.Not.Contain("length changed"));
        }

        [Test]
        public void GetColumnChangeDescription_MaxLengthChange_ReportsLengthChanged()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsNullable = true
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 100,
                IsNullable = true
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("length changed from 50 to 100"));
            Assert.That(result, Does.Not.Contain("type changed"));
        }

        [Test]
        public void GetColumnChangeDescription_MaxLengthChangeToNull_ReportsCorrectly()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsNullable = true
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = null,
                IsNullable = true
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("length changed from 50 to NULL"));
        }

        [Test]
        public void GetColumnChangeDescription_MaxLengthChangeFromNull_ReportsCorrectly()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = null,
                IsNullable = true
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsNullable = true
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("length changed from NULL to 50"));
        }

        [Test]
        public void GetColumnChangeDescription_PrecisionAndScaleChange_ReportsCorrectly()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "DECIMAL",
                Precision = 10,
                Scale = 2,
                IsNullable = true
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "DECIMAL",
                Precision = 18,
                Scale = 4,
                IsNullable = true
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("precision/scale changed from (10,2) to (18,4)"));
        }

        [Test]
        public void GetColumnChangeDescription_NullableChange_ReportsNullabilityChanged()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsNullable = true
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsNullable = false
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("nullability changed from NULL to NOT NULL"));
        }

        [Test]
        public void GetColumnChangeDescription_PrimaryKeyAdded_ReportsAddedToPrimaryKey()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "INT",
                IsNullable = false,
                IsPrimaryKey = false
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "INT",
                IsNullable = false,
                IsPrimaryKey = true
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("added to primary key"));
        }

        [Test]
        public void GetColumnChangeDescription_PrimaryKeyRemoved_ReportsRemovedFromPrimaryKey()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "INT",
                IsNullable = false,
                IsPrimaryKey = true
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "INT",
                IsNullable = false,
                IsPrimaryKey = false
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("removed from primary key"));
        }

        [Test]
        public void GetColumnChangeDescription_UniqueConstraintAdded_ReportsAddedUniqueConstraint()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsUnique = false
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsUnique = true
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("added unique constraint"));
        }

        [Test]
        public void GetColumnChangeDescription_UniqueConstraintRemoved_ReportsRemovedUniqueConstraint()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsUnique = true
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsUnique = false
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("removed unique constraint"));
        }

        [Test]
        public void GetColumnChangeDescription_IdentityAdded_ReportsAddedIdentityProperty()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "Id",
                DataType = "INT",
                IsIdentity = false
            };

            var target = new SchemaDesignerColumn
            {
                Name = "Id",
                DataType = "INT",
                IsIdentity = true,
                IdentitySeed = 1,
                IdentityIncrement = 1
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("added identity property"));
        }

        [Test]
        public void GetColumnChangeDescription_IdentityRemoved_ReportsRemovedIdentityProperty()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "Id",
                DataType = "INT",
                IsIdentity = true,
                IdentitySeed = 1,
                IdentityIncrement = 1
            };

            var target = new SchemaDesignerColumn
            {
                Name = "Id",
                DataType = "INT",
                IsIdentity = false
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("removed identity property"));
        }

        [Test]
        public void GetColumnChangeDescription_IdentityValuesChanged_ReportsIdentityValuesChanged()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "Id",
                DataType = "INT",
                IsIdentity = true,
                IdentitySeed = 1,
                IdentityIncrement = 1
            };

            var target = new SchemaDesignerColumn
            {
                Name = "Id",
                DataType = "INT",
                IsIdentity = true,
                IdentitySeed = 1000,
                IdentityIncrement = 5
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("identity values changed from (1,1) to (1000,5)"));
        }

        [Test]
        public void GetColumnChangeDescription_CollationChanged_ReportsCollationChanged()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "NVARCHAR",
                MaxLength = 50,
                Collation = "SQL_Latin1_General_CP1_CI_AS"
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "NVARCHAR",
                MaxLength = 50,
                Collation = "Latin1_General_BIN"
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("collation changed from SQL_Latin1_General_CP1_CI_AS to Latin1_General_BIN"));
        }

        [Test]
        public void GetColumnChangeDescription_CollationChangedToNull_ReportsCorrectly()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "NVARCHAR",
                MaxLength = 50,
                Collation = "SQL_Latin1_General_CP1_CI_AS"
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "NVARCHAR",
                MaxLength = 50,
                Collation = null
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("collation changed from SQL_Latin1_General_CP1_CI_AS to NULL"));
        }

        [Test]
        public void GetColumnChangeDescription_MultipleChanges_ReportsAllChanges()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsNullable = true,
                IsUnique = false,
                Collation = "SQL_Latin1_General_CP1_CI_AS"
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "NVARCHAR",
                MaxLength = 100,
                IsNullable = false,
                IsUnique = true,
                Collation = "Latin1_General_BIN"
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Does.Contain("type changed from 'VARCHAR' to 'NVARCHAR'"));
            Assert.That(result, Does.Contain("length changed from 50 to 100"));
            Assert.That(result, Does.Contain("nullability changed from NULL to NOT NULL"));
            Assert.That(result, Does.Contain("added unique constraint"));
            Assert.That(result, Does.Contain("collation changed from SQL_Latin1_General_CP1_CI_AS to Latin1_General_BIN"));
        }

        [Test]
        public void GetColumnChangeDescription_NoChanges_ReturnsEmptyString()
        {
            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsNullable = true
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "VARCHAR",
                MaxLength = 50,
                IsNullable = true
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetColumnChangeDescription_DefaultValueChange_ReportsDefaultValueChanged()
        {
            // Assuming DefaultValue property exists in SchemaDesignerColumn
            // If it doesn't exist, this test would need to be modified or removed

            // Arrange
            var source = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "INT",
                DefaultValue = "0"
            };

            var target = new SchemaDesignerColumn
            {
                Name = "TestColumn",
                DataType = "INT",
                DefaultValue = "1"
            };

            // Act
            var result = SchemaDesignerUpdater.GetColumnChangeDescription(source, target);

            // Assert
            // This assertion might need to be adjusted based on the actual implementation
            Assert.That(result, Does.Contain("default value changed"));
        }

        #endregion

        [Test]
        public void ProcessDroppedTables_NoTableDropped_NoScriptGenerated()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var changeReport = new Dictionary<string, SchemaDesignerReportObject>();
            var migrationScript = new StringBuilder();

            // Act
            SchemaDesignerUpdater.ProcessDroppedTables(sourceSchema, targetSchema, changeReport, migrationScript);

            // Assert
            Assert.That(changeReport.Count, Is.EqualTo(0), "No changes should be reported when no tables are dropped");
            Assert.That(migrationScript.ToString(), Is.Empty, "No script should be generated when no tables are dropped");
        }

        [Test]
        public void ProcessDroppedTables_OneTableDropped_GeneratesDropScript()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>()
            };

            var changeReport = new Dictionary<string, SchemaDesignerReportObject>();
            var migrationScript = new StringBuilder();

            // Act
            SchemaDesignerUpdater.ProcessDroppedTables(sourceSchema, targetSchema, changeReport, migrationScript);

            // Assert
            Assert.That(changeReport.Count, Is.EqualTo(1), "One change should be reported");
            Assert.That(changeReport.ContainsKey(tableId.ToString()), Is.True, "Change report should contain the dropped table ID");
            Assert.That(changeReport[tableId.ToString()].TableState, Is.EqualTo(SchemaDesignerReportTableState.DROPPED), "Table state should be DROPPED");
            Assert.That(migrationScript.ToString(), Does.Contain("DROP TABLE [dbo].[TestTable]"), "Script should contain DROP TABLE statement");
        }

        [Test]
        public void ProcessDroppedTables_TableWithForeignKeys_DropsConstraintsFirst()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>
                        {
                            new SchemaDesignerForeignKey
                            {
                                Name = "FK_TestTable_RefTable",
                                ReferencedSchemaName = "dbo",
                                ReferencedTableName = "RefTable",
                                Columns = new List<string> { "RefId" },
                                ReferencedColumns = new List<string> { "Id" }
                            }
                        }
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>()
            };

            var changeReport = new Dictionary<string, SchemaDesignerReportObject>();
            var migrationScript = new StringBuilder();

            // Act
            SchemaDesignerUpdater.ProcessDroppedTables(sourceSchema, targetSchema, changeReport, migrationScript);

            // Assert
            Assert.That(changeReport.Count, Is.EqualTo(1), "One change should be reported");

            var scriptText = migrationScript.ToString();
            Assert.That(scriptText, Does.Contain("DROP CONSTRAINT [FK_TestTable_RefTable]"),
                "Script should contain DROP CONSTRAINT statement for foreign key");
            Assert.That(scriptText, Does.Contain("DROP TABLE [dbo].[TestTable]"),
                "Script should contain DROP TABLE statement");

            // Verify order: DROP CONSTRAINT should come before DROP TABLE
            int constraintIndex = scriptText.IndexOf("DROP CONSTRAINT");
            int tableIndex = scriptText.IndexOf("DROP TABLE");
            Assert.That(constraintIndex, Is.LessThan(tableIndex),
                "DROP CONSTRAINT should come before DROP TABLE in the script");
        }

        [Test]
        public void ProcessDroppedTables_MultipleTablesDropped_GeneratesMultipleDropScripts()
        {
            // Arrange
            var table1Id = Guid.NewGuid();
            var table2Id = Guid.NewGuid();
            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = table1Id,
                        Schema = "dbo",
                        Name = "TestTable1",
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    },
                    new SchemaDesignerTable
                    {
                        Id = table2Id,
                        Schema = "sales",
                        Name = "TestTable2",
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>()
            };

            var changeReport = new Dictionary<string, SchemaDesignerReportObject>();
            var migrationScript = new StringBuilder();

            // Act
            SchemaDesignerUpdater.ProcessDroppedTables(sourceSchema, targetSchema, changeReport, migrationScript);

            // Assert
            Assert.That(changeReport.Count, Is.EqualTo(2), "Two changes should be reported");
            Assert.That(changeReport.ContainsKey(table1Id.ToString()), Is.True, "Change report should contain the first dropped table ID");
            Assert.That(changeReport.ContainsKey(table2Id.ToString()), Is.True, "Change report should contain the second dropped table ID");

            var scriptText = migrationScript.ToString();
            Assert.That(scriptText, Does.Contain("DROP TABLE [dbo].[TestTable1]"), "Script should contain DROP TABLE statement for first table");
            Assert.That(scriptText, Does.Contain("DROP TABLE [sales].[TestTable2]"), "Script should contain DROP TABLE statement for second table");
        }

        [Test]
        public void ProcessDroppedTables_SomeTablesDroppedSomeKept_OnlyGeneratesScriptsForDroppedTables()
        {
            // Arrange
            var keptTableId = Guid.NewGuid();
            var droppedTableId = Guid.NewGuid();
            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = keptTableId,
                        Schema = "dbo",
                        Name = "KeptTable",
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    },
                    new SchemaDesignerTable
                    {
                        Id = droppedTableId,
                        Schema = "dbo",
                        Name = "DroppedTable",
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = keptTableId,
                        Schema = "dbo",
                        Name = "KeptTable",
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    }
                }
            };

            var changeReport = new Dictionary<string, SchemaDesignerReportObject>();
            var migrationScript = new StringBuilder();

            // Act
            SchemaDesignerUpdater.ProcessDroppedTables(sourceSchema, targetSchema, changeReport, migrationScript);

            // Assert
            Assert.That(changeReport.Count, Is.EqualTo(1), "One change should be reported");
            Assert.That(changeReport.ContainsKey(droppedTableId.ToString()), Is.True, "Change report should contain the dropped table ID");
            Assert.That(changeReport.ContainsKey(keptTableId.ToString()), Is.False, "Change report should not contain the kept table ID");

            var scriptText = migrationScript.ToString();
            Assert.That(scriptText, Does.Contain("DROP TABLE [dbo].[DroppedTable]"), "Script should contain DROP TABLE statement for dropped table");
            Assert.That(scriptText, Does.Not.Contain("KeptTable"), "Script should not contain any reference to the kept table");
        }

        [Test]
        public void ProcessDroppedTables_TableWithMultipleForeignKeys_DropsAllConstraintsFirst()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var sourceSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>
                {
                    new SchemaDesignerTable
                    {
                        Id = tableId,
                        Schema = "dbo",
                        Name = "TestTable",
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>
                        {
                            new SchemaDesignerForeignKey
                            {
                                Name = "FK_TestTable_RefTable1",
                                ReferencedSchemaName = "dbo",
                                ReferencedTableName = "RefTable1",
                                Columns = new List<string> { "RefId1" },
                                ReferencedColumns = new List<string> { "Id" }
                            },
                            new SchemaDesignerForeignKey
                            {
                                Name = "FK_TestTable_RefTable2",
                                ReferencedSchemaName = "dbo",
                                ReferencedTableName = "RefTable2",
                                Columns = new List<string> { "RefId2" },
                                ReferencedColumns = new List<string> { "Id" }
                            }
                        }
                    }
                }
            };

            var targetSchema = new SchemaDesignerModel
            {
                Tables = new List<SchemaDesignerTable>()
            };

            var changeReport = new Dictionary<string, SchemaDesignerReportObject>();
            var migrationScript = new StringBuilder();

            // Act
            SchemaDesignerUpdater.ProcessDroppedTables(sourceSchema, targetSchema, changeReport, migrationScript);

            // Assert
            Assert.That(changeReport.Count, Is.EqualTo(1), "One table change should be reported");
            Assert.That(changeReport[tableId.ToString()].ActionsPerformed.Count, Is.EqualTo(3),
                "Three actions should be reported: two FK drops and one table drop");

            var scriptText = migrationScript.ToString();
            Assert.That(scriptText, Does.Contain("DROP CONSTRAINT [FK_TestTable_RefTable1]"),
                "Script should contain DROP CONSTRAINT statement for first foreign key");
            Assert.That(scriptText, Does.Contain("DROP CONSTRAINT [FK_TestTable_RefTable2]"),
                "Script should contain DROP CONSTRAINT statement for second foreign key");
            Assert.That(scriptText, Does.Contain("DROP TABLE [dbo].[TestTable]"),
                "Script should contain DROP TABLE statement");

            // Verify all foreign keys are dropped before the table
            int lastConstraintIndex = scriptText.LastIndexOf("DROP CONSTRAINT");
            int tableIndex = scriptText.IndexOf("DROP TABLE");
            Assert.That(lastConstraintIndex, Is.LessThan(tableIndex),
                "All DROP CONSTRAINT statements should come before DROP TABLE in the script");
        }


        #region Helper Methods

        private SchemaDesignerColumn CreateColumn(Guid id, string name, string dataType, bool isPrimaryKey = false, bool isNullable = true)
        {
            return new SchemaDesignerColumn
            {
                Id = id,
                Name = name,
                DataType = dataType,
                IsPrimaryKey = isPrimaryKey,
                IsNullable = isNullable
            };
        }

        private SchemaDesignerTable CreateTable(Guid id, string schema, string name, List<SchemaDesignerColumn> columns = null, List<SchemaDesignerForeignKey> foreignKeys = null)
        {
            return new SchemaDesignerTable
            {
                Id = id,
                Schema = schema,
                Name = name,
                Columns = columns ?? new List<SchemaDesignerColumn>(),
                ForeignKeys = foreignKeys ?? new List<SchemaDesignerForeignKey>()
            };
        }

        #endregion
    }
}