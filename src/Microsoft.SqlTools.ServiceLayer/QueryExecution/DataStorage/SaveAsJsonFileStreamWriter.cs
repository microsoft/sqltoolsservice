using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class SaveAsJsonFileStreamWriter : SaveAsStreamWriter, IDisposable
    {

        private SaveResultsAsJsonRequestParams saveParams;
        private readonly StreamWriter streamWriter;
        private readonly JsonWriter jsonWriter;

        public SaveAsJsonFileStreamWriter(Stream stream, SaveResultsAsJsonRequestParams requestParams)
            : base(stream, requestParams)
        {
            // Setup the internal state
            saveParams = requestParams;
            streamWriter = new StreamWriter(stream);
            jsonWriter = new JsonTextWriter(streamWriter);

            // Write the header of the file
            jsonWriter.WriteStartArray();
        }

        public override void WriteRow(IList<DbCellValue> row, IList<DbColumnWrapper> columns)
        {
            // Write the header for the object
            jsonWriter.WriteStartObject();
            
            // Write the items out as properties
            int columnStart = ColumnStartIndex ?? 0;
            int columnEnd = ColumnCount ?? columns.Count;
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

        public new void Dispose()
        {
            // Write the footer of the file
            jsonWriter.WriteEndArray();

            jsonWriter.Close();
            streamWriter.Dispose();
            base.Dispose();
        }
    }
}
