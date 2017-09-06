//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Class used for storing results and how the results are to be serialized
    /// </summary>
    public class SaveResultsInfo
    {
        /// <summary>
        /// String representation of the type that service is supposed to serialize to
        ///  E.g. "json" or "csv"
        /// </summary>
        public string SaveFormat { get; set; }

        /// <summary>
        /// Path to file that the serialized results will be stored in
        /// </summary>
        public string SavePath { get; set; }

        /// <summary>
        /// Results that are to be serialized into 'SaveFormat' format
        /// </summary>
        public DbCellValue[][] Rows { get; set; }

        /// <summary>
        /// Whether the current set of Rows passed in is the last for this file
        // </summary>
        public bool IsLast { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SaveResultsInfo(string saveFormat, 
            string savePath, 
            DbCellValue[][] rows, 
            bool isLast)
        {
            this.SaveFormat = saveFormat;
            this.SavePath = savePath;
            this.Rows = Rows;
            this.IsLast = isLast;
        }
    }

    public class SaveAsRequest
    {
        public static readonly
            RequestType<SaveResultsInfo, SaveResultRequestResult> Type =
            RequestType<SaveResultsInfo, SaveResultRequestResult>.Create("query/saveAs");
    }
}
