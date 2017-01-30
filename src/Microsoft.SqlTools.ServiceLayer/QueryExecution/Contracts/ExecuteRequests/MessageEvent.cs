//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests
{
    /// <summary>
    /// Parameters to be sent back with a message notification
    /// </summary>
    public class MessageParams
    {
        /// <summary>
        /// URI for the editor that owns the query
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The message that is being returned
        /// </summary>
        public ResultMessage Message { get; set; }
    }

    public class MessageEvent
    {
        public static readonly
            EventType<MessageParams> Type =
            EventType<MessageParams>.Create("query/message");
    }
}
