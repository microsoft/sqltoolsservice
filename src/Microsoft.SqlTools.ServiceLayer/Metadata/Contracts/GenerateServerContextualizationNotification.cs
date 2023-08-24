//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GenerateServerContextualizationParams
    {
        /// <summary>
        /// The URI of the connection to generate context for.
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Event set after a connection to a server is completed.
    /// </summary>
    public class GenerateServerContextualizationNotification
    {
        public static readonly EventType<GenerateServerContextualizationParams> Type =
            EventType<GenerateServerContextualizationParams>.Create("metadata/generateServerContext");
    }
}
