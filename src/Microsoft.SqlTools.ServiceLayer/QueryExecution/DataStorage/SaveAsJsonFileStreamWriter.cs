// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Writer for writing rows of results to a JSON file.
    /// </summary>
    /// <remarks>
    /// This implements its own IDisposable because the cleanup logic closes the array that was
    /// created when the writer was created. Since this behavior is different than the standard
    /// file stream cleanup, the extra Dispose method was added.
    /// </remarks>
    public class SaveAsJsonFileStreamWriter : SaveAsStreamWriter, IDisposable
    {
        #region Member Variables

        private readonly StreamWriter streamWriter;
        private readonly JsonWriter jsonWriter;

        #endregion

        /// <summary>
        /// Constructor, writes the header to the file, chains into the base constructor
        /// </summary>
        /// <param name="stream">FileStream to access the JSON file output</param>
        /// <param name="requestParams">JSON save as request parameters</param>
        public SaveAsJsonFileStreamWriter(Stream stream, SaveResultsRequestParams requestParams)
            : base(stream, requestParams)
        {
            // Setup the internal state
            streamWriter = new StreamWriter(stream);
            jsonWriter = new JsonTextWriter(streamWriter);
            jsonWriter.Formatting = Formatting.Indented;

            // Write the header of the file
            jsonWriter.WriteStartArray();
        }

        /// <summary>
        /// Writes a row of data as a JSON object
        /// </summary>
        /// <param name="row">The data of the row to output to the file</param>
        /// <param name="columns">
        /// The entire list of columns for the result set. They will be filtered down as per the
        /// request params.
        /// </param>
        public override void WriteRow(IList<DbCellValue> row, IList<DbColumnWrapper> columns)
        {
            // Write the header for the object
            jsonWriter.WriteStartObject();

            // Write the items out as properties
            int columnStart = ColumnStartIndex ?? 0;
            int columnEnd = (ColumnEndIndex != null) ? ColumnEndIndex.Value + 1 : columns.Count;
            for (int i = columnStart; i < columnEnd; i++)
            {
                jsonWriter.WritePropertyName(columns[i].ColumnName);
                if (row[i].RawObject == null)
                {
                    jsonWriter.WriteNull();
                }
                else
                {
                    jsonWriter.WriteValue(row[i].DisplayValue);
                }
            }

            // Write the footer for the object
            jsonWriter.WriteEndObject();
        }

        private bool disposed = false;
        /// <summary>
        /// Disposes the writer by closing up the array that contains the row objects
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Write the footer of the file
                jsonWriter.WriteEndArray();
                // This closes the underlying stream, so we needn't call close on the underlying stream explicitly
                jsonWriter.Close();
            }
            disposed = true;
            base.Dispose(disposing);
        }
    }
}
