//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    internal class SaveResults
    {

        /// Method ported from SSMS

        /// <summary>
        /// Encodes a single field for inserting into a CSV record. The following rules are applied:
        /// <list type="bullet">
        /// <item><description>All double quotes (") are replaced with a pair of consecutive double quotes</description></item>
        /// </list>
        /// The entire field is also surrounded by a pair of double quotes if any of the following conditions are met:
        /// <list type="bullet">
        /// <item><description>The field begins or ends with a space</description></item>
        /// <item><description>The field begins or ends with a tab</description></item>
        /// <item><description>The field contains the ListSeparator string</description></item>
        /// <item><description>The field contains the '\n' character</description></item>
        /// <item><description>The field contains the '\r' character</description></item>
        /// <item><description>The field contains the '"' character</description></item>
        /// </list>
        /// </summary>
        /// <param name="field">The field to encode</param>
        /// <returns>The CSV encoded version of the original field</returns>
        internal static String EncodeCsvField(String field)
        {
            StringBuilder sbField = new StringBuilder(field);

            //Whether this field has special characters which require it to be embedded in quotes
            bool embedInQuotes = false;

            //Check for leading/trailing spaces
            if (sbField.Length > 0 &&
                (sbField[0] == ' ' ||
                sbField[0] == '\t' ||
                sbField[sbField.Length - 1] == ' ' ||
                sbField[sbField.Length - 1] == '\t'))
            {
                embedInQuotes = true;
            }
            else
            {   //List separator being in the field will require quotes
                if (field.Contains(","))
                {
                    embedInQuotes = true;
                }
                else
                {
                    for (int i = 0; i < sbField.Length; ++i)
                    {
                        //Check whether this character is a special character
                        if (sbField[i] == '\r' ||
                            sbField[i] == '\n' ||
                            sbField[i] == '"')
                        { //If even one character requires embedding the whole field will
                            //be embedded in quotes so we can just break out now
                            embedInQuotes = true;
                            break;
                        }
                    }
                }
            }

            //Replace all quotes in the original field with double quotes
            sbField.Replace("\"", "\"\"");

            String ret = sbField.ToString();

            if (embedInQuotes)
            {
                ret = "\"" + ret + "\"";
            }

            return ret;
        }

        /// <summary>
        /// Check if request is a subset of result set or whole result set
        /// </summary>
        /// <param name="saveParams"> Parameters from the request </param>
        /// <returns></returns>
        internal static bool IsSaveSelection(SaveResultsRequestParams saveParams)
        {
            return (saveParams.ColumnStartIndex != null && saveParams.ColumnEndIndex != null
                && saveParams.RowEndIndex != null && saveParams.RowEndIndex != null);
        }

        /// <summary>
        /// Save results as JSON format to the file specified in saveParams
        /// </summary>
        /// <param name="saveParams"> Parameters from the request </param>
        /// <param name="requestContext"> Request context for save results </param>
        /// <param name="result"> Result query object </param>
        /// <returns></returns>
        internal static Task SaveResultsAsJson(SaveResultsAsJsonRequestParams saveParams, RequestContext<SaveResultRequestResult> requestContext, Query result)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using (StreamWriter jsonFile = new StreamWriter(File.Open(saveParams.FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read)))
                    using (JsonWriter jsonWriter = new JsonTextWriter(jsonFile))
                    {
                        ResultSet selectedResultSet;
                        int rowCount = 0;
                        int rowStartIndex = 0;
                        int columnStartIndex = 0;
                        int columnEndIndex = 0;

                        jsonWriter.Formatting = Formatting.Indented;
                        jsonWriter.WriteStartArray();

                        // get the requested resultSet from query
                        lock(result)
                        { 
                            Batch selectedBatch = result.Batches[saveParams.BatchIndex];
                            selectedResultSet = selectedBatch.ResultSets.ToList()[saveParams.ResultSetIndex];                         

                            // set column, row counts depending on whether save request is for entire result set or a subset
                            if (IsSaveSelection(saveParams))
                            {

                                rowCount = saveParams.RowEndIndex.Value - saveParams.RowStartIndex.Value + 1;
                                rowStartIndex = saveParams.RowStartIndex.Value;
                                columnStartIndex = saveParams.ColumnStartIndex.Value;
                                columnEndIndex = saveParams.ColumnEndIndex.Value + 1; // include the last column
                            }
                            else
                            {
                                    rowCount = (int)selectedResultSet.RowCount;
                                    columnEndIndex = selectedResultSet.Columns.Length;
                            }
                        }
                        // retrieve rows and write as json
                        ResultSetSubset resultSubset = await result.GetSubset(saveParams.BatchIndex, saveParams.ResultSetIndex, rowStartIndex, rowCount);

                        lock(selectedResultSet) lock(resultSubset)
                        {
                            foreach (var row in resultSubset.Rows)
                            {
                                jsonWriter.WriteStartObject();
                                for (int i = columnStartIndex; i < columnEndIndex; i++)
                                {
                                    //get column name
                                    DbColumnWrapper col = selectedResultSet.Columns[i];
                                    string val = row[i]?.ToString();
                                    jsonWriter.WritePropertyName(col.ColumnName);
                                    if (val == null)
                                    {
                                        jsonWriter.WriteNull();
                                    }
                                    else
                                    {
                                        jsonWriter.WriteValue(val);
                                    }
                                }
                                jsonWriter.WriteEndObject();
                            }
                        }
                        jsonWriter.WriteEndArray();
                    }

                    await requestContext.SendResult(new SaveResultRequestResult { Messages = null });
                }
                catch (Exception ex)
                {
                    // Delete file when exception occurs
                    if (File.Exists(saveParams.FilePath))
                    {
                        File.Delete(saveParams.FilePath);
                    }
                    await requestContext.SendError(ex.Message);
                }
            });
        }

        /// <summary>
        /// Save results as CSV format to the file specified in saveParams
        /// </summary>
        /// <param name="saveParams"> Parameters from the request </param>
        /// <param name="requestContext">  Request context for save results </param>
        /// <param name="result"> Result query object </param>
        /// <returns></returns>
        internal static Task SaveResultsAsCsv(SaveResultsAsCsvRequestParams saveParams, RequestContext<SaveResultRequestResult> requestContext, Query result)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using (StreamWriter csvFile = new StreamWriter(File.Open(saveParams.FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read)))
                    {
                        ResultSet selectedResultSet;
                        int columnCount = 0;
                        int rowCount = 0;
                        int columnStartIndex = 0;
                        int rowStartIndex = 0;

                        lock(result)
                        {
                            // get the requested resultSet from query
                            Batch selectedBatch = result.Batches[saveParams.BatchIndex];
                            selectedResultSet = (selectedBatch.ResultSets.ToList())[saveParams.ResultSetIndex];
                        }
                        lock(selectedResultSet)
                        {
                            // set column, row counts depending on whether save request is for entire result set or a subset
                            if (IsSaveSelection(saveParams))
                            {
                                columnCount = saveParams.ColumnEndIndex.Value - saveParams.ColumnStartIndex.Value + 1;
                                rowCount = saveParams.RowEndIndex.Value - saveParams.RowStartIndex.Value + 1;
                                columnStartIndex = saveParams.ColumnStartIndex.Value;
                                rowStartIndex = saveParams.RowStartIndex.Value;
                            }
                            else
                            {
                                columnCount = selectedResultSet.Columns.Length;
                                rowCount = (int)selectedResultSet.RowCount;
                            }

                            // write column names if include headers option is chosen
                            if (saveParams.IncludeHeaders)
                            {
                                csvFile.WriteLine(string.Join(",", selectedResultSet.Columns.Skip(columnStartIndex).Take(columnCount).Select(column =>
                                                EncodeCsvField(column.ColumnName) ?? string.Empty)));
                            }
                        }
                        // retrieve rows and write as csv
                        ResultSetSubset resultSubset = await result.GetSubset(saveParams.BatchIndex, saveParams.ResultSetIndex, rowStartIndex, rowCount);
                        lock(resultSubset)
                        {
                            foreach (var row in resultSubset.Rows)
                            {
                                csvFile.WriteLine(string.Join(",", row.Skip(columnStartIndex).Take(columnCount).Select(field =>
                                                EncodeCsvField((field != null) ? field.ToString() : "NULL"))));
                            }
                        }

                    }

                    // Successfully wrote file, send success result
                    await requestContext.SendResult(new SaveResultRequestResult { Messages = null });
                }
                catch (Exception ex)
                {
                    // Delete file when exception occurs
                    if (File.Exists(saveParams.FilePath))
                    {
                        File.Delete(saveParams.FilePath);
                    }
                    await requestContext.SendError(ex.Message);
                }
            });
        }
    }

}