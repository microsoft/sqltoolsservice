using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.InsightsGenerator
{
    class SignatureGenerator
    {
        private DataArray Table;
        private SignatureGeneratorResult Result;
        public SignatureGenerator(DataArray table)
        {
            this.Table = table;
            Result = new SignatureGeneratorResult();
        }

        public SignatureGeneratorResult Learn()
        {
            
            return Result;
        }

        private void TopNItems(int colIndex, int n)
        {
        /*    var sortedCells = SortCellsByColumn(colIndex);
            List<string> items = new List<string>();
            for (int i = 0; i < n && i < sortedCells.Length; i++)
            {
                items.Add(sortedCells[i][colIndex].ToString());
            }
            SignatureGeneratorResult.InsightValues topResults = new SignatureGeneratorResult.InsightValues
            {
                ColumnName = Table.ColumnNames[colIndex],
                Values = items.ToArray()
            };

            Result.Insights.Add(InsightTypes.Average, topResults);
     */   }

        /// <summary>
        /// Getting 
        /// </summary>
        /// <param name="groupIndex"></param>
        /// <param name="colIndex"></param>
        /// <param name="n"></param>
        private void TopNItems(int groupIndex, int colIndex, int n)
        {

        }

        private void BottomNItems(int colIndex, int n)
        {
       /*     var sortedCells = SortCellsByColumn(colIndex);
            List<string> items = new List<string>();
            for (int i = sortedCells.Length - 1; n > 0 && i > 0; i--, n--)
            {
                items.Add(sortedCells[i][colIndex].ToString());
            }
            SignatureGeneratorResult.InsightValues bottomResults = new SignatureGeneratorResult.InsightValues
            {
                ColumnName = Table.ColumnNames[colIndex],
                Values = items.ToArray()
            };

            Result.Insights.Add(InsightTypes.Average, bottomResults);
        */
            }

        private void OverallAverageInsights(int colIndex)
        {
            var outputList = new List<string>();
            outputList.Add("average");
            outputList.Add(CalculateColumnAverage(Table.Cells, colIndex).ToString());
        }

        private void OverallSumInsights(int colIndex)
        {
            var outputList = new List<string>();
            outputList.Add("sum");
            outputList.Add(CalculateColumnSum(Table.Cells, colIndex).ToString());
        }

        private double CalculateColumnAverage(object[][] rows, int colIndex)
        {
            return CalculateColumnSum(rows, colIndex) / rows.Length;
        }

        private double CalculateColumnSum(object[][] rows, int colIndex)
        {
            return rows.Sum(row => double.Parse(row[colIndex].ToString()));
        }

        private object[][] GetRowsBasedOnInputName(string inputName, int colIndex)
        {
            var cellCopy = DeepCloneCells(Table.Cells);
            return cellCopy.Where(row => row[colIndex].ToString() == inputName).ToArray();
        }

        private string[] GetUniqueInputColums(int colIndex)
        {
            var cellCopy = DeepCloneCells(Table.Cells);
            return cellCopy.Select(row => row[colIndex].ToString()).Distinct().ToArray();
        }

        /// <summary>
        /// This function makes a deepclone of the cells and then sorts them according the colIndex
        /// </summary>
        /// <param name="colIndex">The index of the column on which the sort function will work</param>
        /// <returns></returns>
        private object[][] SortCellsByColumn(int colIndex)
        {
            var cellCopy = DeepCloneCells(Table.Cells);
            Comparer<Object> comparer = Comparer<Object>.Default;
            Array.Sort<Object[]>(cellCopy, (x, y) => comparer.Compare(x[colIndex], y[colIndex]));
            return cellCopy;
        }

        public Object[][] DeepCloneCells(object[][] cells)
        {
            var clonedCells = cells.Select(a => a.ToArray()).ToArray();
            return clonedCells;
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
}


/**
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
