//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Parameters for the save results request
    /// </summary>
    public class ResultsToTextRequestParams
    {

        /// <summary>
        /// URI for the editor that called execute results
        /// </summary>
        public string OwnerUri { get; set; }

         /// <summary>
        /// Include headers of columns in text
        /// </summary>
        public bool IncludeHeaders { get; set; }

        /// <summary>
        /// Delimiter for separating data items in text
        /// </summary>
        public string Delimiter { get; set; }

        /// <summary>
        /// either CR, CRLF or LF to seperate rows in text
        /// </summary>
        public string LineSeperator { get; set; }

        /// <summary>
        /// Text identifier for alphanumeric columns in text
        /// </summary>
        public string TextIdentifier { get; set; }

        /// <summary>
        /// Are the results column algined
        /// </summary>
        public bool IsColumnAligned { get; set; }

        /// <summary>
        /// User selected file path where the results need to
        /// be saved if isSave is true, otherwise file path
        /// to save a temp text file to read from
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Are the results needed to be saved
        /// </summary>
        public bool IsSave { get; set; }

    }

    public class ResultsToTextResults
    {
        /// <summary>
        /// Messages with the results
        /// </summary>
        public string[] Messages { get; set; }

        /// <summary>
        /// Was the operation successful or not
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Errors if any when carrying out this operation
        /// </summary>
        public string[] Errors { get; set; }

    }

    public class ResultsToTextRequest
    {
        public static readonly
            RequestType<ResultsToTextRequestParams, ResultsToTextResults> Type = 
            RequestType<ResultsToTextRequestParams, ResultsToTextResults>.Create("query/resultstotext");
    }
}
