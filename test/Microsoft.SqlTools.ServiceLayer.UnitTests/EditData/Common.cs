//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
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

        public static EditTableMetadata GetStandardMetadata(DbColumn[] columns, bool isMemoryOptimized = false, int defaultColumns = 0)
        {
            // Create column metadata providers            
            var columnMetas = columns.Select((c, i) => new EditColumnMetadata
            {
                EscapedName = c.ColumnName,
                Ordinal = i,
                DefaultValue = i < defaultColumns ? DefaultValue : null 
            }).ToArray();

            // Create column wrappers
            var columnWrappers = columns.Select(c => new DbColumnWrapper(c)).ToArray();

            // Create the table metadata
            EditTableMetadata editTableMetadata = new EditTableMetadata
            {
                Columns = columnMetas,
                EscapedMultipartName = TableName,
                IsMemoryOptimized = isMemoryOptimized
            };
            editTableMetadata.Extend(columnWrappers);
            return editTableMetadata;
        }

        public static DbColumn[] GetColumns(bool includeIdentity)
        {
            List<DbColumn> columns = new List<DbColumn>();

            if (includeIdentity)
            {
                columns.Add(new TestDbColumn("id") {IsKey = true, IsIdentity = true, IsAutoIncrement = true});
            }

            for (int i = 0; i < 3; i++)
            {
                columns.Add(new TestDbColumn($"col{i}"));
            }
            return columns.ToArray();
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

        public static void AddCells(RowEditBase rc, int colsToSkip)
        {
            // Skip the first column since if identity, since identity columns can't be updated
            for (int i = colsToSkip; i < rc.AssociatedResultSet.Columns.Length; i++)
            {
                rc.SetCell(i, "123");
            }
        }
    }
}
