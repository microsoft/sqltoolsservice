//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Hosting.Extensibility
{
    /// <summary>
    /// Standard Metadata needed for extensions.
    /// </summary>
    public interface IStandardMetadata
    {       
        /// <summary>
        /// Extension version. Should be in the format "1.0.0.0" or similar
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Unique Id used to identify the export.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Optional Display name describing the export type 
        /// </summary>
        string DisplayName { get; }
    }
}
