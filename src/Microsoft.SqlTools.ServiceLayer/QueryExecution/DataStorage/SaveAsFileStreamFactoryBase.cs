using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public abstract class SaveAsFileStreamFactoryBase<TSaveParams> : ISaveAsFileStreamFactory
    {
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

            FileStreamFunc = fileStreamFunc;
            QueryExecutionSettings = queryExecutionSettings;
            SaveRequestParams = saveRequestParams;
        }

        #region Properties

        protected Func<string, FileMode, FileAccess, FileShare, Stream> FileStreamFunc { get; }

        /// <summary>
        /// Settings for query execution
        /// </summary>
        protected QueryExecutionSettings QueryExecutionSettings { get; }

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
                FileStreamFunc(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                QueryExecutionSettings);
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
            return FileStreamFunc(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        }
    }
}