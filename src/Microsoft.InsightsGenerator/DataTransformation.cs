//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.InsightsGenerator
{
    public class DataTransformer
    {
        private class ColumnInfo
        {
            public int ColumnIndex { get; set; }

            public int DistinctValues { get; set; }

            public DataArray.DataType DataType { get; set; }
        }

        public DataArray Transform(DataArray array)
        {
            if (array == null || array.Cells == null || array.Cells.Length == 0)
            {
                return array;
            }

            array.TransformedColumnNames = GetColumnLabels(array);
            return array;
        }

        private string[] GetColumnLabels(DataArray array)
        {
            int columnCount = array.Cells[0].Length;
            Dictionary<DataArray.DataType, List<ColumnInfo>> columnInfo = new Dictionary<DataArray.DataType, List<ColumnInfo>>();
            for (int column = 0; column < columnCount; ++column)
            {
                int distinctValues;
                DataArray.DataType dataType = GetColumnType(array, column, out distinctValues);
                if (!columnInfo.ContainsKey(dataType))
                {
                    columnInfo.Add(dataType, new List<ColumnInfo>());
                }

                columnInfo[dataType].Add(new ColumnInfo()
                {
                    ColumnIndex = column,
                    DistinctValues = distinctValues,
                    DataType = dataType
                });
            }

            bool containsDateTime = columnInfo.ContainsKey(DataArray.DataType.DateTime);
            string[] labels = new string[columnCount];
            if (containsDateTime)
            {
                List<ColumnInfo> dateColumns = columnInfo[DataArray.DataType.DateTime];
                for (int i = 0; i < dateColumns.Count; ++i)
                {
                    labels[dateColumns[i].ColumnIndex] = "input_t_" + i;
                }
            }

            if (columnInfo.ContainsKey(DataArray.DataType.String))
            {
                int startingIndex = 0;
                List<ColumnInfo> stringColumns = columnInfo[DataArray.DataType.String];
                if (stringColumns.Count > 1)
                {
                    labels[stringColumns[startingIndex].ColumnIndex] = "input_g_0";
                    ++startingIndex;
                }

                for (int i = 0; i < stringColumns.Count - startingIndex; ++i)
                {
                    labels[stringColumns[i + startingIndex].ColumnIndex] = "slicer_" + i;
                }
            }

            if (columnInfo.ContainsKey(DataArray.DataType.Number))
            {
                List<ColumnInfo> numberColumns = columnInfo[DataArray.DataType.Number];
                for (int i = 0; i < numberColumns.Count; ++i)
                {
                    labels[numberColumns[i].ColumnIndex] = "output_" + i;
                }
            }

            return labels;
        }

        private DataArray.DataType GetColumnType(DataArray array, int column, out int distinctValues)
        {
            // count number of distinct values
            HashSet<object> values = new HashSet<object>();
            for (int row = 0; row < array.Cells.Length; ++row) 
            {
                if (!values.Contains(array.Cells[row][column]))
                {
                    values.Add(array.Cells[row][column]);
                }
            }
            distinctValues = values.Count;

            // return the provided type if available 
            if (array.ColumnDataType != null && array.ColumnDataType.Length > column)
            {
                return array.ColumnDataType[column];
            }
            else
            {
                // determine the type from the first value in array
                object firstValue = array.Cells[0][column];
                string firstValueString = firstValue.ToString();

                long longValue;
                double doubleValue;
                if (long.TryParse(firstValueString, out longValue) || double.TryParse(firstValueString, out doubleValue))
                {
                    return DataArray.DataType.Number;
                }
    
                DateTime dateValue;
                if (DateTime.TryParse(firstValueString, out dateValue))
                {
                    return DataArray.DataType.DateTime;
                }

                return DataArray.DataType.String;
            }
        }
    }
}
