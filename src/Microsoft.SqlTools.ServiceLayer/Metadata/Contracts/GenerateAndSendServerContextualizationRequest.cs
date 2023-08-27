//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GenerateAndSendServerContextualizationParams
    {
        /// <summary>
        /// The URI to generate server contextualization scripts for, if needed, and to retrieve for.
        /// </summary>
        public string OwnerUri { get; set; }
    }

    public class GenerateAndSendServerContextualizationResult
    {
        /// <summary>
        /// The array containing the generated server scripts.
        /// </summary>
        public string[] Context { get; set; }
    }

    /// <summary>
    /// Contains the server name and associated context for that server.
    /// </summary>
    public class ServerContextualization
    {
        /// <summary>
        /// The name of the server to generate context for.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// The newly generated server context for the server.
        /// </summary>
        public string[]? NewlyGeneratedContext { get; set; }
    }

    public class GenerateAndSendServerContextualizationRequest
    {
        public static RequestType<GenerateAndSendServerContextualizationParams, GenerateAndSendServerContextualizationResult> Type =
            RequestType<GenerateAndSendServerContextualizationParams, GenerateAndSendServerContextualizationResult>.Create("metadata/generateAndSendServerContextualizationRequest");
    }
}
