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
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Base class for SaveAs file stream factories.
    /// </summary>
    /// <typeparam name="TSaveParams">Type of the save as request parameters</typeparam>
    public abstract class SaveAsFileStreamFactoryBase<TSaveParams> : ISaveAsFileStreamFactory
    {
        private readonly Func<string, FileMode, FileAccess, FileShare, Stream> fileStreamFunc;
        private readonly QueryExecutionSettings queryExecutionSettings;

        /// <summary>
        /// Initializes a new instance of <see cref="SaveAsFileStreamFactoryBase{TSaveParams}"/>.
        /// </summary>
        /// <param name="queryExecutionSettings">Settings used to execute the query</param>
        /// <param name="saveRequestParams">Parameters of the SaveAs request</param>
        /// <param name="fileStreamFunc">Function for creating a FileStream</param>
        protected SaveAsFileStreamFactoryBase(
            QueryExecutionSettings queryExecutionSettings,
            TSaveParams saveRequestParams,
            Func<string, FileMode, FileAccess, FileShare, Stream> fileStreamFunc)
        {
            Validate.IsNotNull(nameof(saveRequestParams), saveRequestParams);
            Validate.IsNotNull(nameof(fileStreamFunc), fileStreamFunc);

            this.fileStreamFunc = fileStreamFunc;
            this.queryExecutionSettings = queryExecutionSettings;
            SaveRequestParams = saveRequestParams;
        }

        #region Properties

        /// <summary>
        /// Parameters for the save as CSV request
        /// </summary>
        protected TSaveParams SaveRequestParams { get; }

         #endregion

        /// <summary>
        /// Returns a new service buffer reader for reading results back in from the temporary
        /// buffer files, file share is ReadWrite to allow concurrent reads/writes to the file.
        /// </summary>
        /// <param name="fileName">Path to the temp buffer file</param>
        /// <returns>Stream reader</returns>
        public IFileStreamReader GetReader(string fileName)
        {
            return new ServiceBufferFileStreamReader(
                fileStreamFunc(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                queryExecutionSettings);
        }

        /// <summary>
        /// Returns a new writer for writing results to the desired file type. File share is
        /// ReadWrite to allow concurrent reads/writes to the file.
        /// </summary>
        /// <param name="fileName">Path to the CSV output file</param>
        /// <param name="columns">The list of columns to output</param>
        /// <returns>Stream writer</returns>
        public abstract ISaveAsFileStreamWriter GetWriter(string fileName, IReadOnlyList<DbColumnWrapper> columns);

        /// <summary>
        /// Safely deletes the file.
        /// </summary>
        /// <param name="fileName">Path to the file to delete</param>
        public void DisposeFile(string fileName)
        {
            FileUtilities.SafeFileDelete(fileName);
        }

        protected Stream GetOutputStream(string fileName)
        {
            return fileStreamFunc(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        }
    }
}