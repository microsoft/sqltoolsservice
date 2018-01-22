//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts
{
    public class ProviderDetails
    {
        private const string ProtocolVersion = "1.0";

        public ProviderDetails()
        {
            ProviderProtocolVersion = ProtocolVersion;
        }
        
        public string ProviderDescription { get; set; }
        public string ProviderName { get; set; }
        public string ProviderProtocolVersion { get; set; }
    }
}