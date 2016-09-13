//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
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
        /// The encoding of the file to save results in
        /// </summary>
        public string FileEncoding { get; set; }

        /// <summary>
        /// Include headers of columns in CSV
        /// </summary>
        public bool IncludeHeaders { get; set; }

        /// <summary>
        /// Index of the batch to get the results from
        /// </summary>
        public int BatchIndex { get; set; }

        /// <summary>
        /// Index of the result set to get the results from
        /// </summary>
        public int ResultSetIndex { get; set; }

        /// <summary>
        /// CSV - Write values in quotes 
        /// </summary>
        public Boolean ValueInQuotes { get; set; }

        /// <summary>
        /// URI for the editor that called save results
        /// </summary>
        public string OwnerUri { get; set; }
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

    public class SaveResultsAsCsvRequest
    {
        public static readonly
            RequestType<SaveResultsRequestParams, SaveResultRequestResult> Type =
            RequestType<SaveResultsRequestParams, SaveResultRequestResult>.Create("query/save");
    }

}