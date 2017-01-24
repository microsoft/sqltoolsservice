//------------------------------------------------------------------------------
// <copyright file="IBatchEventsHandler.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal interface IBatchEventsHandler
    {
        /// <summary>
        /// fired when there is an error message from the server
        /// </summary>
        void OnBatchError(object sender, BatchErrorEventArgs args);

        /// <summary>
        /// fired when there is a message from the server
        /// </summary>
        void OnBatchMessage(object sender, BatchMessageEventArgs args);

        /// <summary>
        /// fired when there is a new result set available. It is guarnteed
        /// to be fired from the same thread that called Execute method
        /// </summary>
        void OnBatchResultSetProcessing(object sender, BatchResultSetEventArgs args);

        /// <summary>
        /// fired when we've done absolutely all actions for the current result set
        /// </summary>
        void OnBatchResultSetFinished(object sender, EventArgs args);

        /// <summary>
        /// fired when the batch recieved cancel request BEFORE it 
        /// initiates cancel operation. Note that it is fired from a
        /// different thread then the one used to kick off execution
        /// </summary>
        void OnBatchCancelling(object sender, EventArgs args);
    }
}