//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using ExecutionEngineCode;
    using ServiceLayer;

    /// <summary>
    /// Wraps the SMO Batch parser to make it a easily useable component.
    /// </summary>
    public sealed class BatchParserWrapper : IDisposable
    {
        #region Private members

        private List<Tuple<int /*startLine*/, int/*startColumn*/>> startLineColumns;
        private List<int /*length*/> lengths;
        private ExecutionEngine executionEngine;
        private BatchEventNotificationHandler notificationHandler;

        #endregion

        /// <summary>
        /// Helper method used to Convert line/column information in a file to offset
        /// </summary>
        private static List<BatchDefinition> ConvertToBatchDefinitionList(string content,
            IList<System.Tuple<int, int>> positions, List<int> lengths)
        {

            List<BatchDefinition> batchDefinitionList = new List<BatchDefinition>();

            if (!string.IsNullOrEmpty(content) && (positions.Count > 0))
            {
                // Instantiate a string reader for the whole sql content
                using (StringReader reader = new StringReader(content))
                {

                    // Generate the first batch definition list
                    int startLine = positions[0].Item1 + 1;
                    int endLine = startLine;
                    int lineDifference = 0;
                    int endColumn;
                    int lineStartOffset = 0;
                    int offset = 0;
                    int startColumn = positions[0].Item2;
                    int count = positions.Count;
                    string batchText = content.Substring(offset, lengths[0]);

                    // if there's only one batch then the line difference is just 0
                    if (count > 1)
                    {
                        lineDifference = positions[1].Item1 - positions[0].Item1;
                    }

                    // get endLine, endColumn for the current batch and the lineStartOffset for the next batch
                    System.Tuple<int, int, int> batchInfo = ReadLines(reader, lineStartOffset, lineDifference, endLine);
                    endLine = batchInfo.Item1;
                    endColumn = batchInfo.Item2;
                    lineStartOffset = batchInfo.Item3;

                    offset = lineStartOffset + positions[0].Item2;

                    // create a new BatchDefinition and add it to the list
                    BatchDefinition batchDef = new BatchDefinition(
                        batchText,
                        startLine,
                        endLine,
                        startColumn + 1,
                        endColumn
                    );

                    batchDefinitionList.Add(batchDef);

                    // Generate the rest batch definitions
                    for (int index = 1; index < count - 1; index++)
                    {
                        lineDifference = positions[index + 1].Item1 - positions[index].Item1;
                        batchInfo = ReadLines(reader, lineStartOffset, lineDifference, endLine);
                        endLine = batchInfo.Item1;
                        endColumn = batchInfo.Item2;
                        lineStartOffset = batchInfo.Item3;
                        batchText = content.Substring(offset, lengths[index]);
                        offset = lineStartOffset + positions[index].Item2;
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
                        batchText = content.Substring(offset, lengths[count - 1]);
                        BatchDefinition lastBatchDef = GetLastBatchDefinition(reader, positions[count - 1], batchText);
                        batchDefinitionList.Add(lastBatchDef);
                    }

                }
            }
            return batchDefinitionList;
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
        private static Tuple<int, int, int> GetBatchDetails(StringReader reader, int endLine)
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
            return Tuple.Create(endLine, endColumn, 0);
        }

        /// <summary>
        /// Read number of lines and get the line offset
        /// </summary>
        private static Tuple<int, int, int> ReadLines(StringReader reader, int lineStartOffset, int n, int endLine)
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
                        ++lineStartOffset;
                        ++endLine;
                        break;
                    }
                    else // regular char just increase
                    {
                        ++endColumn;
                        ++lineStartOffset;
                    }
                }

            }

            return Tuple.Create(endLine, endColumn, lineStartOffset);
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

                    if (batchText.EndsWith(Environment.NewLine, StringComparison.Ordinal) == false 
                        && batchText.EndsWith("\n", StringComparison.Ordinal) == true )
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

            #region IBatchEventHandlers Members

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

            #endregion
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
