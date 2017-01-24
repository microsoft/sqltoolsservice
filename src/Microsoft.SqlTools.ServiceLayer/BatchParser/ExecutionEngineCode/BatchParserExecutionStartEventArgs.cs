//------------------------------------------------------------------------------
// <copyright file="BatchParserExecutionStartEventArgs.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class BatchParserExecutionStartEventArgs : EventArgs
    {
        private BatchParserExecutionStartEventArgs() 
        {
        }

        public BatchParserExecutionStartEventArgs(TextSpan textSpan, Batch batch)            
        {
            _batch = batch;
            _textSpan = textSpan;
        }

        public Batch Batch
        {
            get
            {
                return _batch;
            }
        }

        public TextSpan TextSpan
        {
            get
            {
                return _textSpan;
            }
        }

        #region Private members
        private readonly Batch _batch = null;
        private readonly TextSpan _textSpan;
        #endregion
    }
}