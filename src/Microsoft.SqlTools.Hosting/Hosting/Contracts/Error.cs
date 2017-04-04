//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.SqlTools.Hosting.Contracts
{
    /// <summary>
    /// Defines the message contract for errors returned via SendError.
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Error code. If omitted will default to 0
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Optional information to return with the error
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }
    }
}