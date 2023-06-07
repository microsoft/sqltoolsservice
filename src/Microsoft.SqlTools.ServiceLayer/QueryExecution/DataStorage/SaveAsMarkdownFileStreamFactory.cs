//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class SaveAsMarkdownFileStreamFactory : IFileStreamFactory
    {
        private readonly SaveResultsAsMarkdownRequestParams _saveRequestParams;

        /// <summary>
        /// Constructs and initializes a new instance of <see cref="SaveAsMarkdownFileStreamFactory"/>.
        /// </summary>
        /// <param name="requestParams">Parameters for the save as request</param>
        public SaveAsMarkdownFileStreamFactory(SaveResultsAsMarkdownRequestParams requestParams)
        {
            this._saveRequestParams = requestParams;
        }

        /// <inheritdoc />
        public QueryExecutionSettings QueryExecutionSettings { get; set; }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Throw at all times.</exception>
        [Obsolete("Not implemented for export factories.")]
        public string CreateFile()
        {
            throw new InvalidOperationException("CreateFile not implemented for export factories");
        }

        /// <inheritdoc />
        /// <remarks>
        /// Returns an instance of the <see cref="ServiceBufferFileStreamreader"/>.
        /// </remarks>
        public IFileStreamReader GetReader(string fileName)
        {
            return new ServiceBufferFileStreamReader(
                new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                this.QueryExecutionSettings);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Returns an instance of the <see cref="SaveAsMarkdownFileStreamWriter"/>.
        /// </remarks>
        public IFileStreamWriter GetWriter(string fileName, IReadOnlyList<DbColumnWrapper> columns)
        {
            return new SaveAsMarkdownFileStreamWriter(
                new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite),
                this._saveRequestParams,
                columns);
        }

        /// <inheritdoc />
        public void DisposeFile(string fileName)
        {
            FileUtilities.SafeFileDelete(fileName);
        }
    }
}