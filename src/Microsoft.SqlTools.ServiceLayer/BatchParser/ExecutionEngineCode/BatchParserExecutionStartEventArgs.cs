//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
            this.batch = batch;
            this.textSpan = textSpan;
        }

        public Batch Batch
        {
            get
            {
                return batch;
            }
        }

        public TextSpan TextSpan
        {
            get
            {
                return textSpan;
            }
        }

        #region Private members
        private readonly Batch batch = null;
        private readonly TextSpan textSpan;
        #endregion
    }
}