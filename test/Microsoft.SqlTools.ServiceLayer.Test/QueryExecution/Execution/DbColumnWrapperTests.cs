//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.Execution
{
    /// <summary>
    /// DbColumnWrapper tests
    /// </summary>
    public class DbColumnWrapperTests
    {
        /// <summary>
        /// Test DbColumn derived class
        /// </summary>
        class TestColumn : DbColumn
        {
            public TestColumn(
                string dataTypeName = null, 
                int? columnSize = null, 
                string columnName = null,
                string udtAssemblyQualifiedName = null)
            {
                if (!string.IsNullOrEmpty(dataTypeName))
                {
                    this.DataTypeName = dataTypeName;
                }

                if (columnSize.HasValue)
                {
                    this.ColumnSize = columnSize;
                }

                if (!string.IsNullOrEmpty(columnName))
                {
                    this.ColumnName = columnName;
                }

                if (!string.IsNullOrEmpty(udtAssemblyQualifiedName))
                {
                    this.UdtAssemblyQualifiedName = udtAssemblyQualifiedName;
                }
            }
        }

        /// <summary>
        /// Basic data type and properites test
        /// </summary>
        [Fact]
        public void DataTypeAndPropertiesTest()
        {
            // check that data types array contains items
            var serverDataTypes = DbColumnWrapper.AllServerDataTypes;
            Assert.True(serverDataTypes.Count > 0);
            
            // check default constructor doesn't throw
            Assert.NotNull(new DbColumnWrapper());

            // check various properties are either null or not null
            var column = new TestColumn();
            var wrapper = new DbColumnWrapper(column);
            Assert.NotNull(wrapper.DataType);
            Assert.Null(wrapper.AllowDBNull);
            Assert.Null(wrapper.BaseCatalogName);
            Assert.Null(wrapper.BaseColumnName);
            Assert.Null(wrapper.BaseServerName);
            Assert.Null(wrapper.BaseTableName);
            Assert.Null(wrapper.ColumnOrdinal);
            Assert.Null(wrapper.ColumnSize);
            Assert.Null(wrapper.IsAliased);
            Assert.Null(wrapper.IsAutoIncrement);
            Assert.Null(wrapper.IsExpression);
            Assert.Null(wrapper.IsHidden);
            Assert.Null(wrapper.IsIdentity);
            Assert.Null(wrapper.IsKey);
            Assert.Null(wrapper. IsReadOnly);
            Assert.Null(wrapper.IsUnique);
            Assert.Null(wrapper.NumericPrecision);
            Assert.Null(wrapper.NumericScale);
            Assert.Null(wrapper.UdtAssemblyQualifiedName); 	        
            Assert.Null(wrapper.DataTypeName);
        }

        /// <summary>
        /// constructor test 
        /// </summary>
        [Fact]
        public void DbColumnConstructorTests()
        {
            // check that various constructor parameters initial the wrapper correctly
            var w1 = new DbColumnWrapper(new TestColumn("varchar", int.MaxValue, "Microsoft SQL Server 2005 XML Showplan"));
            Assert.True(w1.IsXml);

            var w2 = new DbColumnWrapper(new TestColumn("binary"));
            Assert.True(w2.IsBytes);

            var w3 = new DbColumnWrapper(new TestColumn("varbinary", int.MaxValue));
            Assert.True(w3.IsBytes);

            var w4 = new DbColumnWrapper(new TestColumn("sql_variant"));
            Assert.True(w4.IsSqlVariant);

            var w5 = new DbColumnWrapper(new TestColumn("my_udt"));
            Assert.True(w5.IsUdt);

            var w6 = new DbColumnWrapper(new TestColumn("my_hieracrchy", null, null, "MICROSOFT.SQLSERVER.TYPES.SQLHIERARCHYID"));
            Assert.True(w6.IsUdt);
        }
    }
}
