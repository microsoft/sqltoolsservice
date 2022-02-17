//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Factory for creating a reader/writer pair that will read from the temporary buffer file
    /// and output to a CSV file.
    /// </summary>
    public class SaveAsCsvFileStreamFactory : SaveAsFileStreamFactoryBase<SaveResultsAsCsvRequestParams>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SaveAsCsvFileStreamFactory"/>.
        /// </summary>
        /// <param name="queryExecutionSettings">Settings used to execute the query</param>
        /// <param name="saveRequestParams">Parameters of the SaveAs request</param>
        /// <param name="fileStreamFunc">Function for creating a FileStream</param>
        public SaveAsCsvFileStreamFactory(
            QueryExecutionSettings queryExecutionSettings,
            SaveResultsAsCsvRequestParams saveRequestParams,
            Func<string, FileMode, FileAccess, FileShare, Stream> fileStreamFunc)
            : base(queryExecutionSettings, saveRequestParams, fileStreamFunc)
        {
        }

        /// <summary>
        /// Returns a new CSV writer for writing results to a CSV file.
        /// </summary>
        /// <param name="fileName">Path to the CSV output file</param>
        /// <param name="columns">The list of columns to output</param>
        /// <returns>Stream writer</returns>
        public override ISaveAsFileStreamWriter GetWriter(string fileName, IReadOnlyList<DbColumnWrapper> columns)
        {
            return new SaveAsCsvFileStreamWriter(GetOutputStream(fileName), SaveRequestParams, columns);
        }
    }
}
