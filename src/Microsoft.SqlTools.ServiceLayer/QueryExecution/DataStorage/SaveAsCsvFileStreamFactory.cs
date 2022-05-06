//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Factory for creating a reader/writer pair that will read from the temporary buffer file
    /// and output to a CSV file.
    /// </summary>
    public class SaveAsCsvFileStreamFactory : IFileStreamFactory
    {
        #region Properties

        /// <summary>
        /// Settings for query execution
        /// </summary>
        public QueryExecutionSettings QueryExecutionSettings { get; set; }

        /// <summary>
        /// Parameters for the save as CSV request
        /// </summary>
        public SaveResultsAsCsvRequestParams SaveRequestParams { get; set; }

        #endregion

        /// <summary>
        /// File names are not meant to be created with this factory.
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown all times</exception>
        [Obsolete]
        public string CreateFile()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a new service buffer reader for reading results back in from the temporary buffer files, file share is ReadWrite to allow concurrent reads/writes to the file.
        /// </summary>
        /// <param name="fileName">Path to the temp buffer file</param>
        /// <returns>Stream reader</returns>
        public IFileStreamReader GetReader(string fileName)
        {
            return new ServiceBufferFileStreamReader(
                new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                QueryExecutionSettings
            );
        }

        /// <summary>
        /// Returns a new CSV writer for writing results to a CSV file, file share is ReadWrite to allow concurrent reads/writes to the file.
        /// </summary>
        /// <param name="fileName">Path to the CSV output file</param>
        /// <param name="columns">
        /// The entire list of columns for the result set. They will be filtered down as per the
        /// request params.
        /// </param>
        /// <returns>Stream writer</returns>
        public IFileStreamWriter GetWriter(string fileName, IReadOnlyList<DbColumnWrapper> columns)
        {
            return new SaveAsCsvFileStreamWriter(
                new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite),
                SaveRequestParams,
                columns
            );
        }

        /// <summary>
        /// Safely deletes the file
        /// </summary>
        /// <param name="fileName">Path to the file to delete</param>
        public void DisposeFile(string fileName)
        {
            FileUtilities.SafeFileDelete(fileName);
        }
    }
}
