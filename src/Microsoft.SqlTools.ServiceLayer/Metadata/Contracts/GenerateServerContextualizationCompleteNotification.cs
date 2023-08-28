//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GenerateServerContextualizationCompleteParams
    {
        /// <summary>
        /// The URI identifying the owner of the connection
        /// </summary>
        public string OwnerUri { get; set; }
        /// <summary>
        /// An array containing the generated server context.
        /// </summary>
        public string[]? Context { get; set; }
        /// <summary>
        /// Holds an error message, if errors were encountered while
        /// generating context
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Generate server contextualization complete notification
    /// </summary>
    public class GenerateServerContextualizationCompleteNotification
    {
        public static readonly EventType<GenerateServerContextualizationCompleteParams> Type =
            EventType<GenerateServerContextualizationCompleteParams>.Create("metadata/generateServerContextComplete");
        
    }
}
