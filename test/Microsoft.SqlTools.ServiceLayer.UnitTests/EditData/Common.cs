//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class Common
    {
        public const string OwnerUri = "testFile";
        public const string DefaultValue = "defaultValue";
        public const string TableName = "tbl";

        public static EditInitializeParams BasicInitializeParameters
        {
            get
            {
                return new EditInitializeParams
                {
                    Filters = new EditInitializeFiltering(),
                    ObjectName = "tbl",
                    ObjectType = "tbl"
                };
            }
        }

        public static async Task<EditSession> GetCustomSession(Query q, EditTableMetadata etm)
        {
            // Step 1) Create the Session object
            // Mock metadata factory
            Mock<IEditMetadataFactory> metaFactory = new Mock<IEditMetadataFactory>();
            metaFactory
                .Setup(f => f.GetObjectMetadata(It.IsAny<DbConnection>(), It.IsAny<string[]>(), It.IsAny<string>()))
                .Returns(etm);

            EditSession session = new EditSession(metaFactory.Object);

            // Step 2) Initialize the Session
            // Mock connector that does nothing
            EditSession.Connector connector = () => Task.FromResult<DbConnection>(null);

            // Mock query runner that returns the query we were provided
            EditSession.QueryRunner queryRunner = (s) => Task.FromResult(new EditSession.EditSessionQueryExecutionState(q));

            // Initialize
            session.Initialize(BasicInitializeParameters, connector, queryRunner, () => Task.FromResult(0), (e) => Task.FromResult(0));
            await session.InitializeTask;

            return session;
        }

        public static async Task<Query> GetQuery(DbColumn[] columns, bool includIdentity, int rowCount = 1)
        {
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            q.Batches[0].ResultSets[0] = await GetResultSet(columns, includIdentity, rowCount);
            return q;
        }

        public static async Task<ResultSet> GetResultSet(DbColumn[] columns, bool includeIdentity, int rowCount = 1)
        {
            IEnumerable<object[]> rows = includeIdentity
                ? Enumerable.Repeat(new object[] { "id", "1", "2", "3" }, rowCount)
                : Enumerable.Repeat(new object[] { "1", "2", "3" }, rowCount);
            var testResultSet = new TestResultSet(columns, rows);
            var reader = new TestDbDataReader(new[] { testResultSet }, false);
            var resultSet = new ResultSet(0, 0, MemoryFileSystem.GetFileStreamFactory());
            await resultSet.ReadResultToEnd(reader, CancellationToken.None);
            return resultSet;
        }

        public static DbDataReader GetNewRowDataReader(DbColumn[] columns, bool includeIdentity)
        {
            object[][] rows = includeIdentity
                ? new[] {new object[] {"id", "q", "q", "q"}}
                : new[] {new object[] {"q", "q", "q"}};
            var testResultSet = new TestResultSet(columns, rows);
            return new TestDbDataReader(new [] {testResultSet}, false);
        }

        public static EditTableMetadata GetCustomEditTableMetadata(DbColumn[] columns)
        {
            // Create column metadata providers            
            var columnMetas = columns.Select((c, i) => new EditColumnMetadata
            {
                EscapedName = c.ColumnName,
                Ordinal = i 
            }).ToArray();

            // Create column wrappers
            var columnWrappers = columns.Select(c => new DbColumnWrapper(c)).ToArray();

            // Create the table metadata
            EditTableMetadata editTableMetadata = new EditTableMetadata
            {
                Columns = columnMetas,
                EscapedMultipartName = TableName,
                IsMemoryOptimized = false
            };
            editTableMetadata.Extend(columnWrappers);
            return editTableMetadata;
        }
        
        public static void AddCells(RowEditBase rc, int colsToSkip)
        {
            // Skip the first column since if identity, since identity columns can't be updated
            for (int i = colsToSkip; i < rc.AssociatedResultSet.Columns.Length; i++)
            {
                rc.SetCell(i, "123");
            }
        }
        
        public class TestDbColumnsWithTableMetadata
        {
            public TestDbColumnsWithTableMetadata(bool isMemoryOptimized, bool identityCol, int defaultCols, int nullableCols)
            {
                List<DbColumn> dbColumns = new List<DbColumn>();
                List<DbColumnWrapper> columnWrappers = new List<DbColumnWrapper>();
                List<EditColumnMetadata> columnMetadatas = new List<EditColumnMetadata>();

                int startingOrdinal = 0;
                
                // Add the identity column at the front of the table
                if (identityCol)
                {
                    const string colName = "id";
                    
                    DbColumn dbColumn = new TestDbColumn(colName)
                    {
                        IsKey = true,
                        IsIdentity = true,
                        IsAutoIncrement = true
                    };
                    EditColumnMetadata columnMetadata = new EditColumnMetadata
                    {
                        EscapedName = colName,
                        Ordinal = startingOrdinal,
                        DefaultValue = null
                    };
                    dbColumns.Add(dbColumn);
                    columnWrappers.Add(new DbColumnWrapper(dbColumn));
                    columnMetadatas.Add(columnMetadata);

                    startingOrdinal++;
                }
                
                // Add each column to the table
                for (int i = startingOrdinal; i < 3 + startingOrdinal; i++)
                {
                    string colName = $"col{i}";
                    DbColumn dbColumn;
                    EditColumnMetadata columnMetadata;
                    
                    if (i < defaultCols + startingOrdinal)
                    {
                        // This column will have a default value
                        dbColumn = new TestDbColumn(colName) {AllowDBNull = false};
                        columnMetadata = new EditColumnMetadata
                        {
                            EscapedName = colName,
                            Ordinal = i,
                            DefaultValue = DefaultValue
                        };
                    }
                    else if (i < nullableCols + defaultCols + startingOrdinal)
                    {
                        // This column will be nullable
                        dbColumn = new TestDbColumn(colName) {AllowDBNull = true};
                        columnMetadata = new EditColumnMetadata
                        {
                            EscapedName = colName,
                            Ordinal = i,
                            DefaultValue = null
                        };
                    }
                    else
                    {
                        // This column doesn't have a default value or is nullable
                        dbColumn = new TestDbColumn(colName) {AllowDBNull = false};
                        columnMetadata = new EditColumnMetadata
                        {
                            EscapedName = colName,
                            Ordinal = i,
                            DefaultValue = null
                        };
                    }
                    dbColumns.Add(dbColumn);
                    columnWrappers.Add(new DbColumnWrapper(dbColumn));
                    columnMetadatas.Add(columnMetadata);
                }
                
                // Put together the table metadata
                EditTableMetadata editTableMetadata = new EditTableMetadata
                {
                    Columns = columnMetadatas.ToArray(),
                    EscapedMultipartName = TableName,
                    IsMemoryOptimized = isMemoryOptimized
                };
                editTableMetadata.Extend(columnWrappers.ToArray());

                DbColumns = dbColumns.ToArray();
                TableMetadata = editTableMetadata;
            }
            
            public DbColumn[] DbColumns { get; }
            public EditTableMetadata TableMetadata { get; }
        }
    }
}
