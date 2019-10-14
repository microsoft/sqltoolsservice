//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Factory that creates file reader/writers that process rows in an internal, non-human readable file format
    /// </summary>
    public class ServiceBufferFileStreamFactory : IFileStreamFactory
    {
        #region Properties

        /// <summary>
        /// The settings for query execution
        /// </summary>
        public QueryExecutionSettings QueryExecutionSettings { get; set; }

        #endregion

        /// <summary>
        ///  Creates a new temporary file
        /// </summary>
        /// <returns>The name of the temporary file</returns>
        public string CreateFile()
        {
            return Path.GetTempFileName();
        }

        /// <summary>
        /// Creates a new <see cref="ServiceBufferFileStreamReader"/> for reading values back from
        /// an SSMS formatted buffer file, file share is ReadWrite to allow concurrent reads/writes to the file.
        /// </summary>
        /// <param name="fileName">The file to read values from</param>
        /// <returns>A <see cref="ServiceBufferFileStreamReader"/></returns>
        public IFileStreamReader GetReader(string fileName)
        {
            return new ServiceBufferFileStreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), QueryExecutionSettings);
        }

        /// <summary>
        /// Creates a new <see cref="ServiceBufferFileStreamWriter"/> for writing values out to an
        /// SSMS formatted buffer file, file share is ReadWrite to allow concurrent reads/writes to the file.
        /// </summary>
        /// <param name="fileName">The file to write values to</param>
        /// <returns>A <see cref="ServiceBufferFileStreamWriter"/></returns>
        public IFileStreamWriter GetWriter(string fileName)
        {
            return new ServiceBufferFileStreamWriter(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite), QueryExecutionSettings);
        }

        /// <summary>
        /// Disposes of a file created via this factory
        /// </summary>
        /// <param name="fileName">The file to dispose of</param>
        public void DisposeFile(string fileName)
        {
            FileUtilities.SafeFileDelete(fileName);
        }
    }
}
