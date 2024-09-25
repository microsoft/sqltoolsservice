//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Copilot.Contracts
{
    /// <summary>
    /// Parameters to the <see cref="FindNodesRequest"/>.
    /// </summary>
    public class StartConversationParams
    {
        public string ConversationUri { get; set; }

        public string ConnectionUri { get; set; }

        public string UserText { get; set; }
    }

    public class StartConversationResponse
    {
        /// <summary>
        /// Information describing the matching nodes in the tree
        /// </summary>
        public bool Success { get; set; }
    }
   

    /// <summary>
    /// TODO
    /// </summary>
    public class StartConversationRequest
    {
        public static readonly
            RequestType<StartConversationParams, StartConversationResponse> Type =
            RequestType<StartConversationParams, StartConversationResponse>.Create("copilot/startconversation");
    }
}
