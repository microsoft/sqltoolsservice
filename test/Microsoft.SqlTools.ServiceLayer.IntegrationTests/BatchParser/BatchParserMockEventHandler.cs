//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;
using System.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.BatchParser
{
    public class BatchParserMockEventHandler : IBatchEventsHandler
    {
        public SqlError Error { get; private set; }

        /// <summary>
        /// fired when there is an error message from the server
        /// </summary>
        public void OnBatchError(object sender, BatchErrorEventArgs args)
        {
            Debug.WriteLine("{0}", args.Message);
            Error = args.Error;
        }

        /// <summary>
        /// fired when there is a message from the server
        /// </summary>
        public void OnBatchMessage(object sender, BatchMessageEventArgs args)
        {
            Debug.WriteLine("{0}", args.Message);
        }

        /// <summary>
        /// fired when there is a new result set available. It is guarnteed
        /// to be fired from the same thread that called Execute method
        /// </summary>
        public void OnBatchResultSetProcessing(object sender, BatchResultSetEventArgs args) { }

        /// <summary>
        /// fired when we've done absolutely all actions for the current result set
        /// </summary>
        public void OnBatchResultSetFinished(object sender, EventArgs args) { }

        /// <summary>
        /// fired when the batch recieved cancel request BEFORE it 
        /// initiates cancel operation. Note that it is fired from a
        /// different thread then the one used to kick off execution
        /// </summary>
        public void OnBatchCancelling(object sender, EventArgs args) { }
    }
}
