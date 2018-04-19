//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    /// <summary>
    /// When using generic SQL queries to retrieve EditData rows, the columns in the result set may be
    /// a reordered subset of the columns that are present in the complete table metadata. 
    /// EditTableMetadata.FilterColumnMetadata() filters out unnecessary columns from the retrieved
    /// table metadata, and reorders the metadata columns so that it matches the same column
    /// ordering in the result set.
    /// </summary>
    public class FilterMetadataTest
    {
        [Fact]
        public void BasicFilterTest()
        {
            EditColumnMetadata[] metas = CreateMetadataColumns(new string[] { "[col1]", "[col2]", "[col3]" });
            DbColumnWrapper[] cols = CreateColumnWrappers(new string[] { metas[0].EscapedName, metas[1].EscapedName, metas[2].EscapedName });
 
            EditColumnMetadata[] filteredData = EditTableMetadata.FilterColumnMetadata(metas, cols);
            ValidateFilteredData(filteredData, cols);
        }

        [Fact]
        public void ReorderedResultsTest()
        {
            EditColumnMetadata[] metas = CreateMetadataColumns(new string[] { "[col1]", "[col2]", "[col3]" });
            DbColumnWrapper[] cols = CreateColumnWrappers(new string[] { metas[1].EscapedName, metas[2].EscapedName, metas[0].EscapedName });

            EditColumnMetadata[] filteredData = EditTableMetadata.FilterColumnMetadata(metas, cols);
            ValidateFilteredData(filteredData, cols);
        }

        [Fact]
        public void LessResultColumnsTest()
        {
            EditColumnMetadata[] metas = CreateMetadataColumns(new string[] { "[col1]", "[col2]", "[col3]", "[fillerCol1]", "[fillerCol2]" });
            DbColumnWrapper[] cols = CreateColumnWrappers(new string[] { metas[0].EscapedName, metas[1].EscapedName, metas[2].EscapedName });

            EditColumnMetadata[] filteredData = EditTableMetadata.FilterColumnMetadata(metas, cols);
            ValidateFilteredData(filteredData, cols);
        }

        [Fact]
        public void EmptyDataTest()
        {
            EditColumnMetadata[] metas = new EditColumnMetadata[0];
            DbColumnWrapper[] cols = new DbColumnWrapper[0];

            EditColumnMetadata[] filteredData = EditTableMetadata.FilterColumnMetadata(metas, cols);
            ValidateFilteredData(filteredData, cols);
        }

        private DbColumnWrapper[] CreateColumnWrappers(string[] colNames)
        {
            DbColumnWrapper[] cols = new DbColumnWrapper[colNames.Length];
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i] = new DbColumnWrapper(new TestDbColumn(colNames[i], i));
            }
            return cols;
        }

        private EditColumnMetadata[] CreateMetadataColumns(string[] colNames)
        {
            EditColumnMetadata[] metas = new EditColumnMetadata[colNames.Length];
            for (int i = 0; i < metas.Length; i++)
            {
                metas[i] = new EditColumnMetadata { EscapedName = colNames[i], Ordinal = i };
            }
            return metas;
        }

        private void ValidateFilteredData(EditColumnMetadata[] filteredData, DbColumnWrapper[] cols)
        {
            Assert.Equal(cols.Length, filteredData.Length);
            for (int i = 0; i < cols.Length; i++)
            {
                Assert.Equal(cols[i].ColumnName, filteredData[i].EscapedName);
                if (cols[i].ColumnOrdinal.HasValue)
                {
                    Assert.Equal(cols[i].ColumnOrdinal, filteredData[i].Ordinal);
                }
            }
        }
    }
}
