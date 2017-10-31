//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.AsyncRequest
{
    /// <summary>
    /// The base class for responses that are created async
    /// </summary>
    public class AsyncSqlResponse
    {
        /// <summary>
        /// Error message returned from the engine for a failure reason, if any.
        /// </summary>
        public string ErrorMessage { get; set; }

        public string OwnerUri { get; set; }
    }
}
