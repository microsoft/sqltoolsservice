//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Hosting.Contracts
{
    /// <summary>
    /// Defines the DMP server capabilities
    /// </summary>
    public class DmpServerCapabilities
    {
        public string ProtocolVersion { get; set; }

        public string ProviderName { get; set; }

        public string ProviderDisplayName { get; set; }

        public ConnectionProviderOptions ConnectionProvider { get; set; }
    }
}
