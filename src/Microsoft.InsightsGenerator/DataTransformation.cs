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

            DataArray.DataType[] columnDataType;
            array.TransformedColumnNames = GetColumnLabels(array , out columnDataType);
            array.ColumnDataType = columnDataType;
            return array;
        }

        private string[] GetColumnLabels(DataArray array, out DataArray.DataType[] columnDataType)
        {
            columnDataType = new DataArray.DataType[array.ColumnNames.Length];
            int columnCount = array.Cells[0].Length;
            Dictionary<DataArray.DataType, List<ColumnInfo>> columnInfo = new Dictionary<DataArray.DataType, List<ColumnInfo>>();
            for (int column = 0; column < columnCount; ++column)
            {
                int distinctValues;     
                DataArray.DataType dataType = GetColumnType(array, column, out distinctValues);
                columnDataType[column] = dataType;

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
                if (columnInfo.ContainsKey(DataArray.DataType.String))
                {
                    List<ColumnInfo> stringColumns = columnInfo[DataArray.DataType.String];
                    for (int i = 0; i < stringColumns.Count; ++i)
                    {
                        labels[stringColumns[i].ColumnIndex] = "slicer_" + i;
                    }
                }
            } 
            else
            {
                if (columnInfo.ContainsKey(DataArray.DataType.String))
                {
                    int maxDistinctValue = Int32.MaxValue;
                    int maxColumnIndex = -1;
                    int maxColumnLabelIndex = 0;
                    List<ColumnInfo> stringColumns = columnInfo[DataArray.DataType.String];
                    for (int i = 0; i < stringColumns.Count; ++i)
                    {
                        if (maxDistinctValue == Int32.MaxValue || maxDistinctValue < stringColumns[i].DistinctValues)
                        {
                            maxDistinctValue = stringColumns[i].DistinctValues;
                            maxColumnIndex = i;
                            maxColumnLabelIndex = stringColumns[i].ColumnIndex;
                        }
                    }
                    
                    labels[maxColumnLabelIndex] = "input_g_0";
                    int adjustIndex = 0;
                    for (int i = 0; i < stringColumns.Count; ++i)
                    {
                        if (i != maxColumnIndex)
                        {
                            labels[stringColumns[i].ColumnIndex] = "slicer_" + (i - adjustIndex);
                        }
                        else
                        {
                            ++adjustIndex;
                        }
                    }
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
