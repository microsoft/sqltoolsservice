//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{

    /// <summary>
    /// Class associated with batch parser execution start event
    /// </summary>
    internal class BatchParserExecutionStartEventArgs : EventArgs
    {
        
        private readonly Batch batch = null;
        private readonly TextSpan textSpan;

        private BatchParserExecutionStartEventArgs() 
        {
        }

        /// <summary>
        /// Contructor method for the class
        /// </summary>
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

    }
}