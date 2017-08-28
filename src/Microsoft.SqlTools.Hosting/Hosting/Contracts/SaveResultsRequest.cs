//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.Hosting.Contracts
{
    /// <summary>
    /// Parameters for the save results request
    /// </summary>
    public class SaveResultsRequestParams
    {
        /// <summary>
        /// The path of the file to save results in
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Index of the batch to get the results from
        /// </summary>
        public int BatchIndex { get; set; }

        /// <summary>
        /// Index of the result set to get the results from
        /// </summary>
        public int ResultSetIndex { get; set; }

        /// <summary>
        /// URI for the editor that called save results
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Start index of the selected rows (inclusive)
        /// </summary>
        public int? RowStartIndex { get; set; }

        /// <summary>
        /// End index of the selected rows (inclusive)
        /// </summary>
        public int? RowEndIndex { get; set; }
        
        /// <summary>
        /// Start index of the selected columns (inclusive)
        /// </summary>
        /// <returns></returns>
        public int? ColumnStartIndex { get; set; }

        /// <summary>
        /// End index of the selected columns (inclusive)
        /// </summary>
        /// <returns></returns>
        public int? ColumnEndIndex { get; set; }

        /// <summary>
        /// Check if request is a subset of result set or whole result set
        /// </summary>
        /// <returns></returns>
        public bool IsSaveSelection
        {
            get
            {
                return ColumnStartIndex.HasValue && ColumnEndIndex.HasValue
                       && RowStartIndex.HasValue && RowEndIndex.HasValue;
            }
        }
    }

    /// <summary>
    /// Parameters to save results as CSV
    /// </summary>
    public class SaveResultsAsCsvRequestParams: SaveResultsRequestParams
    {
        /// <summary>
        /// Include headers of columns in CSV
        /// </summary>
        public bool IncludeHeaders { get; set; }
    }

    /// <summary>
    /// Parameters to save results as Excel
    /// </summary>
    public class SaveResultsAsExcelRequestParams : SaveResultsRequestParams
    {
        /// <summary>
        /// Include headers of columns in Excel 
        /// </summary>
        public bool IncludeHeaders { get; set; }
    }

    /// <summary>
    /// Parameters to save results as JSON
    /// </summary>
    public class SaveResultsAsJsonRequestParams: SaveResultsRequestParams
    {
        //TODO: define config for save as JSON
    }

    /// <summary>
    /// Parameters for the save results result
    /// </summary>
    public class SaveResultRequestResult
    {
        /// <summary>
        /// Error messages for saving to file. 
        /// </summary>
        public string Messages { get; set; }
    }

    /// <summary>
    /// Request type to save results as CSV
    /// </summary>
    public class SaveResultsAsCsvRequest
    {
        public static readonly
            RequestType<SaveResultsAsCsvRequestParams, SaveResultRequestResult> Type =
            RequestType<SaveResultsAsCsvRequestParams, SaveResultRequestResult>.Create("query/saveCsv");
    }

    /// <summary>
    /// Request type to save results as Excel
    /// </summary>
    public class SaveResultsAsExcelRequest
    {
        public static readonly
            RequestType<SaveResultsAsExcelRequestParams, SaveResultRequestResult> Type =
            RequestType<SaveResultsAsExcelRequestParams, SaveResultRequestResult>.Create("query/saveExcel");
    }

    /// <summary>
    /// Request type to save results as JSON
    /// </summary>
    public class SaveResultsAsJsonRequest
    {
        public static readonly
            RequestType<SaveResultsAsJsonRequestParams, SaveResultRequestResult> Type =
            RequestType<SaveResultsAsJsonRequestParams, SaveResultRequestResult>.Create("query/saveJson");
    }

}