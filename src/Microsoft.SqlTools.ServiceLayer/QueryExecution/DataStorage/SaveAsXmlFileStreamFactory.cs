// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class SaveAsXmlFileStreamFactory : IFileStreamFactory
    {

        #region Properties

        /// <summary>
        /// Settings for query execution
        /// </summary>
        public QueryExecutionSettings QueryExecutionSettings { get; set; }

        /// <summary>
        /// Parameters for the save as XML request
        /// </summary>
        public SaveResultsAsXmlRequestParams SaveRequestParams { get; set; }

        #endregion

        /// <summary>
        /// File names are not meant to be created with this factory.
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown all times</exception>
        [Obsolete]
        public string CreateFile()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns a new service buffer reader for reading results back in from the temporary buffer files
        /// </summary>
        /// <param name="fileName">Path to the temp buffer file</param>
        /// <returns>Stream reader</returns>
        public IFileStreamReader GetReader(string fileName)
        {
            return new ServiceBufferFileStreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read), QueryExecutionSettings);
        }

        /// <summary>
        /// Returns a new XML writer for writing results to a XML file
        /// </summary>
        /// <param name="fileName">Path to the XML output file</param>
        /// <returns>Stream writer</returns>
        public IFileStreamWriter GetWriter(string fileName)
        {
            return new SaveAsXmlFileStreamWriter(new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite), SaveRequestParams);
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
