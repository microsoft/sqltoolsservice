//------------------------------------------------------------------------------
// <copyright file="BatchParserWrapper.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
    using SqlTools.ServiceLayer;

    /// <summary>
    /// Wraps the SMO Batch parser to make it a easily useable component.
    /// </summary>
    public sealed class BatchParserWrapper : IDisposable
    {
        public BatchParserWrapper()
        {
            _executionEngine = new ExecutionEngine();

            ////commented for performance reasons
            // subscribe to executionEngine events
            //_executionEngine.ScriptExecutionFinished +=
            //    new EventHandler<ScriptExecutionFinishedEventArgs>(OnScriptExecutionFinished);

            // subscribe to executionEngine BatchParser events
            _executionEngine.BatchParserExecutionError +=
                new EventHandler<BatchParserExecutionErrorEventArgs>(OnBatchParserExecutionError);
            ////commented for performance reasons
            //_executionEngine.BatchParserExecutionStart +=
            //    new System.EventHandler<BatchParserExecutionStartEventArgs>(OnBatchParserExecutionStart);
            _executionEngine.BatchParserExecutionFinished +=
                new System.EventHandler<BatchParserExecutionFinishedEventArgs>(OnBatchParserExecutionFinished);

            // instantiate notificationHandler class
            _notificationHandler = new BatchEventNotificationHandler();
        }

        public List<BatchDefinition> GetBatches(string sqlScript)
        {
            _startLineColumns = new List<System.Tuple<int /*startLine*/, int/*startColumn*/>>();
            _lengths = new List<int /* length */>();

            // execute the script - all communication / integration after here happen via event handlers
            _executionEngine.ParseScript(sqlScript, _notificationHandler);

            List<System.Tuple<int/*startOffset*/, int/*length*/>> positions = new List<System.Tuple<int, int>>();

            IList<int/*startOffset*/> offsets = ConvertToOffsetSortedInput(sqlScript, _startLineColumns);
            Debug.Assert(offsets.Count == _lengths.Count);
            for (int batchIndex = 0, batchCount = offsets.Count; batchIndex < batchCount; batchIndex++)
            {
                positions.Add(new System.Tuple<int/*startOffset*/, int/*length*/>(offsets[batchIndex], _lengths[batchIndex]));
            }

            List<string> batchTextList = GetBatchText(sqlScript, positions);
           
            List<BatchDefinition> batchDefinitions = new List<BatchDefinition>();
            int endLine;
            int endColumn;
            System.Tuple<int, int> endLineColumn;

            for (int i = 0; i < offsets.Count; i++)
            {   
                StringReader reader = new StringReader(sqlScript);

                // if it's the last batch
                if (i == offsets.Count - 1)
                {      
                    endLineColumn = GetLastEndLineColumn(reader);
                    endLine = endLineColumn.Item1;
                    endColumn = endLineColumn.Item2;
                }
                else {
                    endLine = _startLineColumns[i+1].Item1-1;
                    endColumn = GetEndColumn(reader, endLine);
                }

                // create a batch definition for each batch
                BatchDefinition batchDefinition = new BatchDefinition(
                    batchTextList[i], //batchText
                    _startLineColumns[i].Item1 + 1, //startLine
                    endLine, //endLine
                    _startLineColumns[i].Item2 + 1, //startColumn
                    endColumn); //endColumn

                batchDefinitions.Add(batchDefinition);           
            }
         
            _startLineColumns = null;
            _lengths = null;
           
            return batchDefinitions;
        }

        /// <summary>
        /// Helper method used to return the individual text for the batches
        /// </summary>
        /// <param name="sqlScript"></param>
        /// <param name="batchResult"></param>
        /// <returns></returns>
        private static List<string> GetBatchText(string sqlScript,
            List<System.Tuple<int /*startOffset*/, int /*length*/>> batchResult)
        {
            List<string /*batchText*/> batchTextList = new List<string>();
            foreach (var result in batchResult)
            {
                int startOffset = result.Item1;
                int length = result.Item2;
                batchTextList.Add(sqlScript.Substring(startOffset, length));
            }
            return batchTextList;
        }

        /// <summary>
        /// Helper method used to Convert line/column information in a file to offset
        /// </summary>
        /// <param name="content"></param>
        /// <param name="positions"></param>
        /// <returns></returns>
        private static IList<int> ConvertToOffsetSortedInput(string content, IList<System.Tuple<int, int>> positions)
        {
            IList<int/*offset*/> offsetList = new List<int>();

            if (!string.IsNullOrEmpty(content) && (positions.Count > 0))
            {
                // Get char array to find \r and \n
                using (StringReader reader = new StringReader(content))
                {
                    int lineStartOffset = 0;
                    int count = positions.Count;

                    // Calculate the first offset
                    // positions is 1-based
                    int lineDifference = positions[0].Item1;
                    lineStartOffset = ReadLines(reader, lineStartOffset, lineDifference);
                    // offset is 0-based
                    int offset = lineStartOffset + positions[0].Item2;
                    offsetList.Add(offset);

                    // Calculate the rest offsets
                    for (int index = 1; index < count; index++)
                    {
                        lineDifference = positions[index].Item1 - positions[index - 1].Item1;
                        lineStartOffset = ReadLines(reader, lineStartOffset, lineDifference);
                        offset = lineStartOffset + positions[index].Item2;
                        offsetList.Add(offset);
                    }
                }
            }
            return offsetList;
        }

        /// <summar>
        /// Helper method used to get the end column
        /// <param name="reader"></param>
        /// <param name="endLine"></param>
        /// <returns></returns>
        private static int GetEndColumn(StringReader reader, int endLine)
        {
            string endLineString = null;
            int endColumn = 0;
            int ch;
            for (int i = 0; i < endLine; i++) {
                endLineString = reader.ReadLine();
            }
            StringReader endLineReader = new StringReader(endLineString);
            while (true) {
                ch = endLineReader.Read();
                if (ch == -1) 
                {
                    return endColumn;
                }
                endColumn++;
            }
        }

        /// <summary>
        /// Helper method used to get the end line and column for the last batch
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static System.Tuple<int /*endLine */, int /*endColumn */> GetLastEndLineColumn(StringReader reader)
        {
            int endLine = 0;
            int endColumn = 0;
            string nextLine;
            string lastLine = null; // get the last line
            int ch;

            while ((nextLine = reader.ReadLine()) != null)
            {
                endLine++;
                lastLine = nextLine;
            }

            StringReader lastLineReader = new StringReader(lastLine);
                       
            while (true) 
            {
                ch = lastLineReader.Read();
                if (ch == -1) 
                {
                    break;
                }
                endColumn++;
            }
            return System.Tuple.Create(endLine, endColumn);
        }



        /// <summary>
        /// Read number of lines and get the line offset
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="lineStartOffset"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static int ReadLines(StringReader reader, int lineStartOffset, int n)
        {
            Validate.IsNotNull(nameof(reader), reader);

            for (int i = 0; i < n; i++)
            {
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
                        break;
                    }
                    else // regular char just increase
                    {
                        ++lineStartOffset;
                    }
                }
            }
            return lineStartOffset;
        }

        #region ExecutionEngine Event Handlers

        ////commented for performance reasons
        //private void OnScriptExecutionFinished(object sender, ScriptExecutionFinishedEventArgs e)
        //{
        //    if (e != null)
        //    {
        //        Tracer.TraceEvent(TraceEventType.Verbose, TraceId.TSqlModel, 
        //            SchemaResources.BatchParserWrapperScriptExecutionComplete, e.ExecutionResult);
        //    }
        //}

        private void OnBatchParserExecutionError(object sender, BatchParserExecutionErrorEventArgs args)
        {
            if (args != null)
            {

                Logger.Write(LogLevel.Verbose, SR.BatchParserWrapperExecutionError);
                throw new Exception(SR.BatchParserWrapperExecutionEngineError);

            }
        }


        ////commented for performance reasons
        //private void OnBatchParserExecutionStart(object sender, BatchParserExecutionStartEventArgs args).
        //{
        //    if (args != null)
        //    {
        //        Tracer.TraceEvent(TraceEventType.Verbose, TraceId.TSqlModel,
        //            SchemaResources.BatchParserWrapperExecutionStart, args.Batch, args.TextSpan.iStartLine);
        //    }
        //}

        private void OnBatchParserExecutionFinished(object sender, BatchParserExecutionFinishedEventArgs args)
        {
            try
            {
                if (args != null && args.Batch != null)
                {
                    ////commented for performance reasons
                    //Tracer.TraceEvent(TraceEventType.Verbose, TraceId.TSqlModel,
                    //    SchemaResources.BatchParserWrapperExecutionFinished, args.ExecutionResult);

                    System.Tuple<int /*startLine*/, int/*startColumn*/> position = new System.Tuple<int, int>(args.Batch.TextSpan.iStartLine, args.Batch.TextSpan.iStartIndex);


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
                    _startLineColumns.Add(position);
                    _lengths.Add(batchTextLength);
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
            public BatchEventNotificationHandler()
            {
            }

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

        #region Private members

        private List<System.Tuple<int /*startLine*/, int/*startColumn*/>> _startLineColumns;
        private List<int /*length*/> _lengths;
        private ExecutionEngine _executionEngine;
        private BatchEventNotificationHandler _notificationHandler;

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            if (_executionEngine != null)
            {
                _executionEngine.Dispose();
                _executionEngine = null;
                _startLineColumns = null;
                _lengths = null;
            }
        }

        #endregion
    }
}
