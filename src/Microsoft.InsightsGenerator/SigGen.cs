//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

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
            var stringInputIndexes = new List<int>();
            var timeInputIndexes = new List<int>();
            var slicerIndexes = new List<int>();
            var outputIndexes = new List<int>();

            for (var i = 0; i < Table.TransformedColumnNames.Length; i++) 
            {
                if (Table.TransformedColumnNames[i].Contains("input_g"))
                {
                    stringInputIndexes.Add(i);
                }
                if (Table.TransformedColumnNames[i].Contains("input_t"))
                {
                    timeInputIndexes.Add(i);
                }
                if (Table.TransformedColumnNames[i].Contains("slicer"))
                {
                   slicerIndexes.Add(i);
                }
                if (Table.TransformedColumnNames[i].Contains("output"))
                {
                    outputIndexes.Add(i);
                }
            }

            foreach (int stringIndex in stringInputIndexes)
            {
                foreach (int outputIndex in outputIndexes) 
                {
                    ExecuteStringInputInsights(stringIndex, outputIndex);
                    foreach (int slicerIndex in slicerIndexes) 
                    {
                        ExecuteStringInputSlicerInsights(stringIndex, outputIndex, slicerIndex);
                    }
                }    
            }

            return Result;
        }


        public void ExecuteStringInputInsights(int inputCol, int outputCol)
        {
            var n = Table.Cells.Length;
            if (Table.Cells.Length >8) {
                n = 3;
            }
            OverallAverageInsights(outputCol);
            OverallBottomInsights(n, inputCol, outputCol);
            OverallMaxInsights(outputCol);
            OverallMinInsights(outputCol);
            OverallSumInsights(outputCol);
            OverallTopInsights(n, inputCol, outputCol);
            UniqueInputsInsight(inputCol);
        }

        public void ExecuteStringInputSlicerInsights(int inputCol, int outputCol, int slicerCol)
        {
            var n = Table.Cells.Length;
            if (Table.Cells.Length > 8)
            {
                n = 3;
            }
            if (Table.Cells.Length > 50)
            {
                n = 5;
            }

            SlicedMaxInsights(slicerCol, outputCol);
            SlicedAverageInsights(slicerCol, outputCol);
            SlicedBottomInsights(n, inputCol, slicerCol, outputCol);
            SlicedSumInsights(slicerCol, outputCol);
            SlicedPercentageInsights(slicerCol, outputCol);
            SlicedSumInsights(slicerCol, outputCol);
            SlicedMinInsights(slicerCol, outputCol);
            SlicedTopInsights(n, inputCol, slicerCol, outputCol);
        }

        public void UniqueInputsInsight(int inputCol)
        {
            List<string> insight = new List<string>();
            // Adding the insight identifier
            insight.Add(SignatureGeneratorResult.uniqueInputsIdentifier);
            var uniqueInputs = GetUniqueColumValues(inputCol);
            insight.Add(uniqueInputs.Length.ToString());
            insight.AddRange(uniqueInputs);
            Result.Insights.Add(insight);
        }
        public void OverallTopInsights(long n, int inputColumn, int outputColumn)
        {
            List<string> insight = new List<string>();
            // Adding the insight identifier
            insight.Add(SignatureGeneratorResult.topInsightIdentifier);

            insight.AddRange(GenericTop(Table.Cells, n, inputColumn, outputColumn));

            Result.Insights.Add(insight);
        }


        public void SlicedTopInsights(long n, int inputColumn, int sliceColumn, int outputColumn)
        {
            List<string> insight = new List<string>();
            // Adding the insight identifier
            insight.Add(SignatureGeneratorResult.topSliceInsightIdentifier);

            object[] slices = GetUniqueColumValues(sliceColumn);

            insight.Add(slices.Length.ToString());

            foreach (var slice in slices)
            {
                insight.Add(slice.ToString());
                var sliceTable = CreateSliceBucket(sliceColumn, slice.ToString());
                insight.AddRange(GenericTop(sliceTable, n, inputColumn, outputColumn));
            }

            Result.Insights.Add(insight);
        }

        public List<string> GenericTop(Object[][] table, long n, int inputColumn, int outputColumn)
        {
            List<string> insight = new List<string>();

            Object[][] sortedTable = SortCellsByColumn(table, outputColumn);

            double outputSum = CalculateColumnSum(sortedTable, outputColumn);

            for (int i = sortedTable.Length - 1; i >= 0 && i >= sortedTable.Length - n; i--)
            {
                double percent = Percentage(Double.Parse(sortedTable[i][outputColumn].ToString()), outputSum);
                string temp = String.Format("{0} ({1}) {2}%", sortedTable[i][inputColumn].ToString(), sortedTable[i][outputColumn].ToString(), percent);
                insight.Add(temp);
            }

            // Adding the count of the result
            insight.Insert(0, insight.Count.ToString());

            return insight;
        }

        public void OverallBottomInsights(long n, int inputColumn, int outputColumn)
        {
            List<string> insight = new List<string>();
            // Adding the insight identifier
            insight.Add(SignatureGeneratorResult.bottomInsightIdentifier);

            insight.AddRange(GenericBottom(Table.Cells, n, inputColumn, outputColumn));

            Result.Insights.Add(insight);
        }

        public void SlicedBottomInsights(long n, int inputColumn, int sliceColumn, int outputColumn)
        {
            List<string> insight = new List<string>();
            // Adding the insight identifier
            insight.Add(SignatureGeneratorResult.bottomSliceInsightIdentifier);

            object[] slices = GetUniqueColumValues(sliceColumn);

            insight.Add(slices.Length.ToString());

            foreach (var slice in slices)
            {
                insight.Add(slice.ToString());
                var sliceTable = CreateSliceBucket(sliceColumn, slice.ToString());
                insight.AddRange(GenericBottom(sliceTable, n, inputColumn, outputColumn));
            }

            Result.Insights.Add(insight);
        }

        public List<string> GenericBottom(Object[][] table, long n, int inputColumn, int outputColumn)
        {
            List<string> insight = new List<string>();


            Object[][] sortedTable = SortCellsByColumn(table, outputColumn);

            double outputSum = CalculateColumnSum(sortedTable, outputColumn);

            for (int i = 0; i < n && i < sortedTable.Length; i++)
            {
                double percent = Percentage(Double.Parse(sortedTable[i][outputColumn].ToString()), outputSum);
                string temp = String.Format("{0} ({1}) {2}%", sortedTable[i][inputColumn].ToString(), sortedTable[i][outputColumn].ToString(), percent);
                insight.Add(temp);
            }

            // Adding the count of the result
            insight.Insert(0, insight.Count.ToString());

            return insight;
        }

        public void OverallAverageInsights(int colIndex)
        {
            var outputList = new List<string>();
            outputList.Add(SignatureGeneratorResult.averageInsightIdentifier);
            outputList.Add(CalculateColumnAverage(Table.Cells, colIndex).ToString());
            Result.Insights.Add(outputList);
        }

        public void OverallSumInsights(int colIndex)
        {
            var outputList = new List<string>();
            outputList.Add(SignatureGeneratorResult.sumInsightIdentifier);
            outputList.Add(CalculateColumnSum(Table.Cells, colIndex).ToString());
            Result.Insights.Add(outputList);
        }

        public void OverallMaxInsights(int colIndex)
        {
            var outputList = new List<string>();
            outputList.Add(SignatureGeneratorResult.maxInsightIdentifier);
            outputList.Add(CalculateColumnMax(Table.Cells, colIndex).ToString());
            Result.Insights.Add(outputList);
        }

        public void OverallMinInsights(int colIndex)
        {
            var outputList = new List<string>();
            outputList.Add(SignatureGeneratorResult.minInsightIdentifier);
            outputList.Add(CalculateColumnMin(Table.Cells, colIndex).ToString());
            Result.Insights.Add(outputList);
        }

        public void SlicedSumInsights(int sliceIndex, int colIndex)
        {
            var insight = new List<string>();
            insight.Add(SignatureGeneratorResult.sumSliceInsightIdentifier);
            object[] slices = GetUniqueColumValues(sliceIndex);

            insight.Add(slices.Length.ToString());

            foreach (var slice in slices)
            {
                insight.Add(slice.ToString());
                var sliceTable = CreateSliceBucket(sliceIndex, slice.ToString());
                insight.Add(CalculateColumnSum(sliceTable, colIndex).ToString());
            }

            Result.Insights.Add(insight);
        }

        public void SlicedMaxInsights(int sliceIndex, int colIndex)
        {
            var insight = new List<string>();
            insight.Add(SignatureGeneratorResult.maxSliceInsightIdentifier);
            object[] slices = GetUniqueColumValues(sliceIndex);

            insight.Add(slices.Length.ToString());

            foreach (var slice in slices)
            {
                insight.Add(slice.ToString());
                var sliceTable = CreateSliceBucket(sliceIndex, slice.ToString());
                insight.Add(CalculateColumnMax(sliceTable, colIndex).ToString());
            }

            Result.Insights.Add(insight);
        }

        public void SlicedMinInsights(int sliceIndex, int colIndex)
        {
            var insight = new List<string>();
            insight.Add(SignatureGeneratorResult.minSliceInsightIdentifier);
            object[] slices = GetUniqueColumValues(sliceIndex);

            insight.Add(slices.Length.ToString());

            foreach (var slice in slices)
            {
                insight.Add(slice.ToString());
                var sliceTable = CreateSliceBucket(sliceIndex, slice.ToString());
                insight.Add(CalculateColumnMin(sliceTable, colIndex).ToString());
            }

            Result.Insights.Add(insight);
        }

        public void SlicedAverageInsights(int sliceIndex, int colIndex)
        {
            var insight = new List<string>();
            insight.Add(SignatureGeneratorResult.sumSliceInsightIdentifier);
            object[] slices = GetUniqueColumValues(sliceIndex);

            insight.Add(slices.Length.ToString());

            foreach (var slice in slices)
            {
                insight.Add(slice.ToString());
                var sliceTable = CreateSliceBucket(sliceIndex, slice.ToString());
                insight.Add(CalculateColumnAverage(sliceTable, colIndex).ToString());
            }

            Result.Insights.Add(insight);
        }

        public void SlicedPercentageInsights(int sliceIndex, int colIndex)
        {
            var insight = new List<string>();
            insight.Add(SignatureGeneratorResult.percentageSliceInsightIdentifier);
            object[] slices = GetUniqueColumValues(sliceIndex);

            insight.Add(slices.Length.ToString());
            double totalSum = CalculateColumnSum(Table.Cells, colIndex);
            foreach (var slice in slices)
            {
                insight.Add(slice.ToString());
                var sliceTable = CreateSliceBucket(sliceIndex, slice.ToString());
                double sliceSum = CalculateColumnSum(sliceTable, colIndex);
                var percentagePerSlice =  Percentage(sliceSum, totalSum);
                insight.Add(percentagePerSlice.ToString());
            }

            Result.Insights.Add(insight);
        }


        private double CalculateColumnAverage(object[][] rows, int colIndex)
        {
            return Math.Round(CalculateColumnSum(rows, colIndex) / rows.Length, 2);
        }

        private double CalculateColumnSum(object[][] rows, int colIndex)
        {
            return Math.Round(rows.Sum(row => double.Parse(row[colIndex].ToString())), 2);
        }

        private double CalculateColumnPercentage(object[][] rows, int colIndex)
        {
            return rows.Sum(row => double.Parse(row[colIndex].ToString()));
        }
        private double CalculateColumnMin(object[][] rows, int colIndex)
        {
            return rows.Min(row => double.Parse(row[colIndex].ToString()));
        }

        private double CalculateColumnMax(object[][] rows, int colIndex)
        {
            return rows.Max(row => double.Parse(row[colIndex].ToString()));
        }

        private string[] GetUniqueColumValues(int colIndex)
        {
            return Table.Cells.Select(row => row[colIndex].ToString()).Distinct().ToArray();
        }

        public Object[][] CreateSliceBucket(int sliceColIndex, string sliceValue)
        {
            List<Object[]> slicedTable = new List<object[]>();
            foreach (var row in Table.Cells)
            {
                if (row[sliceColIndex].Equals(sliceValue))
                {
                    slicedTable.Add(DeepCloneRow(row));
                }
            }
            return slicedTable.ToArray();
        }

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

        public double Percentage(double value, double sum)
        {
            return Math.Round((double)((value / sum) * 100), 2);
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
    public static string topSliceInsightIdentifier = "topPerSlice";
    public static string bottomSliceInsightIdentifier = "bottomPerSlice";
    public static string averageInsightIdentifier = "average";
    public static string sumInsightIdentifier = "sum";
    public static string maxInsightIdentifier = "max";
    public static string minInsightIdentifier = "min";
    public static string averageSliceInsightIdentifier = "averagePerSlice";
    public static string sumSliceInsightIdentifier = "sumPerSlice";
    public static string percentageSliceInsightIdentifier = "percentagePerSlice";
    public static string maxSliceInsightIdentifier = "maxPerSlice";
    public static string minSliceInsightIdentifier = "minPerSlice";
    public static string uniqueInputsIdentifier = "uniqueInputs";
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
