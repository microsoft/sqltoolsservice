//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Result message object with timestamp and actual message
    /// </summary>
    public class ResultMessage
    {
        /// <summary>
        /// ID of the batch that generated this message. If null, this message
        /// was not generated as part of a batch
        /// </summary>
        public int? BatchId { get; set; }

        /// <summary>
        /// Whether or not this message is an error
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Timestamp of the message
        /// Stored in UTC ISO 8601 format; should be localized before displaying to any user
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// Message contents
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Constructor with default "Now" time
        /// </summary>
        public ResultMessage(string message, bool isError, int? batchId)
        {
            BatchId = batchId;
            IsError = isError;
            Time = DateTime.Now.ToString("o");
            Message = message;
        }

        /// <summary>
        /// Default constructor, used for deserializing JSON RPC only
        /// </summary>
        public ResultMessage()
        {
        }
    }
}