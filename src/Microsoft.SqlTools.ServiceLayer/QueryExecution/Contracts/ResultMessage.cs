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
        /// Timestamp of the message
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// Message contents
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Full constructor
        /// </summary>
        public ResultMessage(string timeStamp, string message)
        {
            Time = timeStamp;
            Message = message;
        }

        /// <summary>
        /// Constructor with default "Now" time
        /// </summary>
        public ResultMessage(string message)
        {
            Time = DateTime.UtcNow.ToString("o") + "Z";
            Message = message;
        }
    }
}