//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.Contracts
{
    /// <summary>
    /// Defines a message that is sent from the client to request
    /// the version of the server.
    /// </summary>
    public class CapabilitiesRequest
    {
        public static readonly
           RequestType<CapabilitiesRequest, CapabilitiesResponse> Type =
            RequestType<CapabilitiesRequest, CapabilitiesResponse>.Create("capabilities/list");

        public string HostName { get; set; }

        public string HostVersion { get; set; }
    }

    public class CapabilitiesResponse
    {
        public DmpServerCapabilities Capabilities { get; set; }
    }    
}
