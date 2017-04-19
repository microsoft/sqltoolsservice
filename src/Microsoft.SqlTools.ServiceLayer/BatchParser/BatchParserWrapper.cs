//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
 
    /// <summary>
    /// Wraps the SMO Batch parser to make it a easily useable component.
    /// </summary>
    public sealed class BatchParserWrapper : IDisposable
    {

        private List<Tuple<int /*startLine*/, int/*startColumn*/>> startLineColumns;
        private List<int /*length*/> lengths;
        private ExecutionEngine executionEngine;
        private BatchEventNotificationHandler notificationHandler;
        
        /// <summary>
        /// Helper method used to Convert line/column information in a file to offset
        /// </summary>
        private static List<BatchDefinition> ConvertToBatchDefinitionList(string content,
            IList<Tuple<int, int>> positions, List<int> lengths)
        {

            List<BatchDefinition> batchDefinitionList = new List<BatchDefinition>();
            if (positions.Count == 0 && lengths.Count == 0) 
            {
                return batchDefinitionList;
            }
            List<int> offsets = GetOffsets(content, positions);

            if (!string.IsNullOrEmpty(content) && (positions.Count > 0))
            {
                // Instantiate a string reader for the whole sql content
                using (StringReader reader = new StringReader(content))
                {

                    // Generate the first batch definition list
                    int startLine = positions[0].Item1 + 1; //positions is 0 index based
                    int endLine = startLine;
                    int lineDifference = startLine - 1;
                    int endColumn;
                    int offset = offsets[0];
                    int startColumn = positions[0].Item2;
                    int count = positions.Count;
                    string batchText = content.Substring(offset, lengths[0]);

                    // if there's only one batch then the line difference is just startLine
                    if (count > 1)
                    {
                        lineDifference = positions[1].Item1 - positions[0].Item1;
                    }

                    // get endLine, endColumn for the current batch and the lineStartOffset for the next batch
                    var batchInfo = ReadLines(reader, lineDifference, endLine);
                    endLine = batchInfo.Item1;
                    endColumn = batchInfo.Item2;

                    // create a new BatchDefinition and add it to the list
                    BatchDefinition batchDef = new BatchDefinition(
                        batchText,
                        startLine,
                        endLine,
                        startColumn + 1,
                        endColumn
                    );

                    batchDefinitionList.Add(batchDef);

                    if (count > 1) 
                    {
                        offset = offsets[1] + positions[0].Item2;
                    }

                    // Generate the rest batch definitions
                    for (int index = 1; index < count - 1; index++)
                    {
                        lineDifference = positions[index + 1].Item1 - positions[index].Item1;
                        batchInfo = ReadLines(reader, lineDifference, endLine);
                        endLine = batchInfo.Item1;
                        endColumn = batchInfo.Item2;
                        offset = offsets[index];
                        batchText = content.Substring(offset, lengths[index]);
                        startLine = positions[index].Item1;
                        startColumn = positions[index].Item2;

                        // make a new batch definition for each batch
                        BatchDefinition batch = new BatchDefinition(
                            batchText,
                            startLine,
                            endLine,
                            startColumn + 1,
                            endColumn
                        );
                        batchDefinitionList.Add(batch);
                    }

                    // if there is only one batch then that was the last one anyway
                    if (count > 1)
                    {

                        batchText = content.Substring(offsets[count-1], lengths[count - 1]);
                        BatchDefinition lastBatchDef = GetLastBatchDefinition(reader, positions[count - 1], batchText);
                        batchDefinitionList.Add(lastBatchDef);
                    }

                }
            }
            return batchDefinitionList;
        }

        private static int GetMaxStartLine(IList<Tuple<int, int>> positions) 
        {
            int highest = 0;
            foreach (var position in positions) 
            {
                if (position.Item1 > highest) 
                {
                    highest = position.Item1;
                }
            }
            return highest;
        }

        /// <summary>
        /// Gets offsets for all batches
        /// </summary>
        private static List<int> GetOffsets(string content, IList<Tuple<int, int>> positions) 
        {

            List<int> offsets = new List<int>();
            int count = 0;
            int offset = 0;
            bool foundAllOffsets = false;
            int maxStartLine = GetMaxStartLine(positions);
            using (StringReader reader = new StringReader(content))
            {
                // go until we have found offsets for all batches
                while (!foundAllOffsets)
                {
                    // go until the last start line of the batches
                    for (int i = 0; i <= maxStartLine ; i++)
                    {
                        // get offset for the current batch
                        ReadLines(reader, ref count, ref offset, ref foundAllOffsets, positions, offsets, i);

                        // if we found all the offsets, then we're done
                        if (foundAllOffsets)
                        {
                            break;
                        }

                    }
                }
            }             
            return offsets;
        }

        /// <summary>
        /// Helper function to read lines of batches to get offsets
        /// </summary>
        private static void ReadLines(StringReader reader, ref int count, ref int offset, ref bool foundAllOffsets, 
                        IList<Tuple<int, int>> positions, List<int> offsets, int iteration)
        {
            int ch;
            while (true)
            {
                if (positions[count].Item1 == iteration)
                {
                    count++;
                    offsets.Add(offset);
                    if (count == positions.Count)
                    {
                        foundAllOffsets = true;
                        break;
                    }
                }
                ch = reader.Read();
                if (ch == -1) // EOF do nothing
                {
                    break;
                }
                else if (ch == 10 /* for \n */) // End of line increase and break
                {
                    offset++;
                    break;
                }
                else // regular char just increase
                {
                    offset++;
                }
            }
        }


        /// <summary>
        /// Helper method to get the last batch 
        /// </summary>
        private static BatchDefinition GetLastBatchDefinition(StringReader reader,
            Tuple<int, int> position, string batchText)
        {
            int startLine = position.Item1;
            int startColumn = position.Item2;
            string prevLine = null;
            string line = reader.ReadLine();
            int endLine = startLine;

            // find end line
            while (line != null)
            {
                endLine++;
                if (line != "\n")
                {
                    prevLine = line;
                }
                line = reader.ReadLine();
            }

            // get number of characters in the last line
            int endColumn = prevLine.ToCharArray().Length;

            return new BatchDefinition(
                batchText,
                startLine,
                endLine,
                startColumn + 1,
                endColumn
            );
        }

        /// <summary>
        /// Helper function to get correct lines and columns
        /// in a single batch with multiple statements
        /// </summary>
        private static Tuple<int, int> GetBatchDetails(StringReader reader, int endLine)
        {
            string prevLine = null;
            string line = reader.ReadLine();

            // find end line
            while (line != null)
            {
                endLine++;
                if (line != "\n")
                {
                    prevLine = line;
                }
                line = reader.ReadLine();
            }

            // get number of characters in the last line
            int endColumn = prevLine.ToCharArray().Length;

            //lineOffset doesn't matter because its the last batch
            return Tuple.Create(endLine, endColumn);
        }

        /// <summary>
        /// Get end line and end column
        /// </summary>
        private static Tuple<int, int> ReadLines(StringReader reader, int n, int endLine)
        {
            Validate.IsNotNull(nameof(reader), reader);
            int endColumn = 0;

            // if only one batch with multiple lines
            if (n == 0)
            {
                return GetBatchDetails(reader, endLine);
            }

            // if there are more than one batch
            for (int i = 0; i < n; i++)
            {
                endColumn = 0;
                int ch;
                while (true)
                {
                    ch = reader.Read();
                    if (ch == -1) // EOF do nothing
                    {
                        break;
                    }
                    else if (ch == 10 /* for \n */) // End of line increase and break
                    {
                        ++endLine;
                        break;
                    }
                    else // regular char just increase
                    {
                        ++endColumn;
                    }
                }

            }

            return Tuple.Create(endLine, endColumn);
        }

        /// <summary>
        /// Wrapper API for the Batch Parser that returns a list of
        /// BatchDefinitions when given a string to parse
        /// </summary>
        public BatchParserWrapper()
        {
            executionEngine = new ExecutionEngine();

            // subscribe to executionEngine BatchParser events
            executionEngine.BatchParserExecutionError += OnBatchParserExecutionError;

            executionEngine.BatchParserExecutionFinished += OnBatchParserExecutionFinished;

            // instantiate notificationHandler class
            notificationHandler = new BatchEventNotificationHandler();
        }

        /// <summary>
        /// Takes in a query string and returns a list of BatchDefinitions
        /// </summary>
        public List<BatchDefinition> GetBatches(string sqlScript)
        {
            startLineColumns = new List<System.Tuple<int /*startLine*/, int /*startColumn*/>>();
            lengths = new List<int /* length */>();

            // execute the script - all communication / integration after here happen via event handlers
            executionEngine.ParseScript(sqlScript, notificationHandler);

            // retrieve a list of BatchDefinitions 
            List<BatchDefinition> batchDefinitionList = ConvertToBatchDefinitionList(sqlScript, startLineColumns,
                lengths);

            return batchDefinitionList;
        }


        #region ExecutionEngine Event Handlers

        private void OnBatchParserExecutionError(object sender, BatchParserExecutionErrorEventArgs args)
        {
            if (args != null)
            {

                Logger.Write(LogLevel.Verbose, SR.BatchParserWrapperExecutionError);
                throw new Exception(SR.BatchParserWrapperExecutionEngineError);

            }
        }

        private void OnBatchParserExecutionFinished(object sender, BatchParserExecutionFinishedEventArgs args)
        {
            try
            {
                if (args != null && args.Batch != null)
                {

                    Tuple<int /*startLine*/, int/*startColumn*/> position = new Tuple<int, int>(args.Batch.TextSpan.iStartLine, args.Batch.TextSpan.iStartIndex);


                    // PS168371
                    //
                    // There is a bug in the batch parser where it appends a '\n' to the end of the last
                    // batch if a GO or statement appears at the end of the string without a \r\n.  This is
                    // throwing off length calculations in other places in the code because the length of
                    // the string returned is longer than the length of the actual string
                    //
                    // To work around this issue we detect this case (a single \n without a preceding \r 
                    // and then adjust the length accordingly
                    string batchText = args.Batch.Text;
                    int batchTextLength = batchText.Length;

                    if (!batchText.EndsWith(Environment.NewLine, StringComparison.Ordinal) 
                        && batchText.EndsWith("\n", StringComparison.Ordinal))
                    {
                        batchTextLength -= 1;
                    }

                    // Add the script info
                    startLineColumns.Add(position);
                    lengths.Add(batchTextLength);
                }
            }
            catch (NotImplementedException)
            {
                // intentionally swallow
            }
            catch (Exception e)
            {
                // adding this for debugging
                Logger.Write(LogLevel.Warning, "Exception Caught in BatchParserWrapper.OnBatchParserExecutionFinished(...)" + e.ToString());
                throw;
            }
        }

        #endregion

        #region Internal BatchEventHandlers class

        /// <summary>
        /// Internal implementation class to implement IBatchEventHandlers
        /// </summary>
        internal class BatchEventNotificationHandler : IBatchEventsHandler
        {
            public void OnBatchError(object sender, BatchErrorEventArgs args)
            {
                if (args != null)
                {

                    Logger.Write(LogLevel.Normal, SR.BatchParserWrapperExecutionEngineError);
                    throw new Exception(SR.BatchParserWrapperExecutionEngineError);

                }
            }

            public void OnBatchMessage(object sender, BatchMessageEventArgs args)
            {
#if DEBUG
                if (args != null)
                {
                    Logger.Write(LogLevel.Normal, SR.BatchParserWrapperExecutionEngineBatchMessage);
                }
#endif
            }

            public void OnBatchResultSetProcessing(object sender, BatchResultSetEventArgs args)
            {
#if DEBUG
                if (args != null && args.DataReader != null)
                {
                    Logger.Write(LogLevel.Normal, SR.BatchParserWrapperExecutionEngineBatchResultSetProcessing);
                }
#endif
            }

            public void OnBatchResultSetFinished(object sender, EventArgs args)
            {
#if DEBUG
                Logger.Write(LogLevel.Normal, SR.BatchParserWrapperExecutionEngineBatchResultSetFinished);
#endif
            }

            public void OnBatchCancelling(object sender, EventArgs args)
            {
                Logger.Write(LogLevel.Normal, SR.BatchParserWrapperExecutionEngineBatchCancelling);
            }
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (executionEngine != null)
                {
                    executionEngine.Dispose();
                    executionEngine = null;
                    startLineColumns = null;
                    lengths = null;
                }
            }
        }
        #endregion
    }
}
