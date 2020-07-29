using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Globalization;


namespace Microsoft.InsightsGenerator
{
    class SignatureGenerator
    {
        private DataArray Table;
        public SignatureGeneratorResult Result;

        public SignatureGenerator(DataArray table)
        {
            this.Table = table;
            Result = new SignatureGeneratorResult();
        }

        public SignatureGeneratorResult Learn()
        {

            return Result;
        }

        public void Top(long n, int inputColumn, int outputColumn)
        {
            List<string> insight = new List<string>();
            // Adding the insight identifier
            insight.Add(SignatureGeneratorResult.topInsightIdentifier);

            insight.AddRange(genericTop(Table.Cells, n, inputColumn, outputColumn));

            Result.Insights.Add(insight);
        }

        public void TopPerSlice(long n, int inputColumn, int sliceColumn, int outputColumn)
        {
            List<string> insight = new List<string>();
            // Adding the insight identifier
            insight.Add(SignatureGeneratorResult.topSliceInsightIdentifier);

            object[] slices = sliceValues(sliceColumn);

            insight.Add(slices.Length.ToString());

            foreach (var slice in slices)
            {
                insight.Add(slice.ToString());
                var sliceTable = createSliceBucket(sliceColumn, slice.ToString());
                insight.AddRange(genericTop(sliceTable, n, inputColumn, outputColumn));
            }

            Result.Insights.Add(insight);
        }

        public List<string> genericTop(Object[][] table, long n, int inputColumn, int outputColumn)
        {
            List<string> insight = new List<string>();

            Object[][] sortedTable = SortCellsByColumn(table, outputColumn);

            long outputSum = sum(sortedTable, outputColumn);

            for (int i = sortedTable.Length - 1; i >= 0 && i >= sortedTable.Length - n; i--)
            {
                double percent = percentage(Double.Parse(sortedTable[i][outputColumn].ToString()), outputSum);
                string temp = String.Format("{0} ({1}) {2}%", sortedTable[i][inputColumn].ToString(), sortedTable[i][outputColumn].ToString(), percent);
                insight.Add(temp);
            }

            // Adding the count of the result
            insight.Insert(0, insight.Count.ToString());

            return insight;
        }

        public void Bottom(long n, int inputColumn, int outputColumn)
        {
            List<string> insight = new List<string>();
            // Adding the insight identifier
            insight.Add(SignatureGeneratorResult.bottomInsightIdentifier);

            insight.AddRange(genericBottom(Table.Cells, n, inputColumn, outputColumn));

            Result.Insights.Add(insight);
        }

        public void BottomPerSlice(long n, int inputColumn, int sliceColumn, int outputColumn)
        {
            List<string> insight = new List<string>();
            // Adding the insight identifier
            insight.Add(SignatureGeneratorResult.bottomSliceInsightIdentifier);

            object[] slices = sliceValues(sliceColumn);

            insight.Add(slices.Length.ToString());

            foreach (var slice in slices)
            {
                insight.Add(slice.ToString());
                var sliceTable = createSliceBucket(sliceColumn, slice.ToString());
                insight.AddRange(genericBottom(sliceTable, n, inputColumn, outputColumn));
            }

            Result.Insights.Add(insight);
        }

        public List<string> genericBottom(Object[][] table, long n, int inputColumn, int outputColumn)
        {
            List<string> insight = new List<string>();


            Object[][] sortedTable = SortCellsByColumn(table, outputColumn);

            long outputSum = sum(sortedTable, outputColumn);

            for(int i = 0; i < n && i < sortedTable.Length; i++)
            {
                double percent = percentage(Double.Parse(sortedTable[i][outputColumn].ToString()), outputSum);
                string temp = String.Format("{0} ({1}) {2}%", sortedTable[i][inputColumn].ToString(), sortedTable[i][outputColumn].ToString(), percent);
                insight.Add(temp);
            }

            // Adding the count of the result
            insight.Insert(0, insight.Count.ToString());

            return insight;
        }


        public double percentage(double value, long sum)
        {
            return Math.Round((double)((value / sum) * 100), 2);
        }

        public long sum(object[][] table, int outputColumn)
        {
            long result = 0;
            foreach(var row in table)
            {
                result += long.Parse(row[outputColumn].ToString());
            }
            return result;
        }

        public Object[] sliceValues(int sliceColumn)
        {
            HashSet<Object> slices = new HashSet<object>();
            foreach(var row in Table.Cells)
            {
                slices.Add(row[sliceColumn]);
            }
            return slices.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sliceColumn">The reference column</param>
        /// <returns></returns>
        public Object[][] createSliceBucket(int sliceColumn, string sliceValue)
        {
            List<Object[]> slicedTable = new List<object[]>();
            foreach(var row in Table.Cells)
            {
                if(row[sliceColumn].Equals(sliceValue))
                {
                    slicedTable.Add(DeepCloneRow(row));
                }
            }
            return slicedTable.ToArray();
        }

        /// <summary>
        /// This function makes a deepclone of the cells and then sorts them according the colIndex
        /// </summary>
        /// <param name="colIndex">The index of the column on which the sort function will work</param>
        /// <returns></returns>
        public object[][] SortCellsByColumn(Object[][] table, int colIndex)
        {
            var cellCopy = DeepCloneTable(table);
            Comparer<Object> comparer = Comparer<Object>.Default;
            switch (this.Table.ColumnDataType[colIndex])
            {
                case DataArray.DataType.Number:
                    Array.Sort<Object[]>(cellCopy, (x, y) => comparer.Compare(double.Parse(x[colIndex].ToString()), double.Parse(y[colIndex].ToString())));
                    break;
                case DataArray.DataType.String:
                    Array.Sort<Object[]>(cellCopy, (x, y) => String.Compare(x[colIndex].ToString(), y[colIndex].ToString()));
                    break;
                case DataArray.DataType.DateTime:
                    Array.Sort<Object[]>(cellCopy, (x, y) => DateTime.Compare(DateTime.Parse(x[colIndex].ToString()), DateTime.Parse(y[colIndex].ToString())));
                    break;

            }
            return cellCopy;
        }

        public Object[][] DeepCloneTable(object[][] table)
        {
            return table.Select(a => a.ToArray()).ToArray();
        }

        public Object[] DeepCloneRow(object[] row)
        {
            return row.Select(a => a).ToArray();
        }
    }
}


public class SignatureGeneratorResult
{
    public SignatureGeneratorResult()
    {
        Insights = new List<List<string>>();
    }
    public List<List<string>> Insights { get; set; }

    public static string topInsightIdentifier = "top";
    public static string bottomInsightIdentifier = "bottom";
    public static string topSliceInsightIdentifier = "topPerSlices";
    public static string bottomSliceInsightIdentifier = "bottomPerSlices";
}



/** Some general format about the output
 * "time"/"string"
 * "top", "3", " input (value) %OfValue ", " input (value) %OfValue ", " input (value) %OfValue "
 * "top", "1", " input (value) %OfValue "
 * "bottom", "3", " input (value) %OfValue ", " input (value) %OfValue ", " input (value) %OfValue "
 * "average", "100"
 * "mean", "100"
 * "median", "100"
 * "averageSlice", "#slice","nameofslice", "100", "nameofslice", "100", "nameofslice", "100"
 * "topPerslice", "#slice", "nameofslice", "3", " input (value) %OfValue ", " input (value) %OfValue ", " input (value) %OfValue ",
 *                           "nameofslice", "3", " input (value) %OfValue ", " input (value) %OfValue ", " input (value) %OfValue ",
 *                           "nameofslice", "3", " input (value) %OfValue ", " input (value) %OfValue ", " input (value) %OfValue "
 *                           ....
 * 
**/
