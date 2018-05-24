//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.AvailabilityGroup;

namespace Microsoft.SqlTools.ServiceLayer.AvailabilityGroup.Contracts
{
    /// <summary>
    /// a class for storing various properties of availability replicas
    /// </summary>
    public class AvailabilityReplicaInfo
    {
        public string Name { get; set; }

        public string Role { get; set; }

        public int RoleValue { get; set; }

        public string AvailabilityMode { get; set; }

        public int AvailabilityModeValue { get; set; }

        public string FailoverMode { get; set; }

        public int FailoverModeValue { get; set; }

        public string ConnectionsInPrimaryRole { get; set; }

        public int ConnectionsInPrimaryRoleValue { get; set; }

        public string ReadableSecondary { get; set; }

        public int ReadableSecondaryValue { get; set; }

        public string SeedingMode { get; set; }

        public int SeedingModeValue { get; set; }

        public bool IsSupported_SeedingMode { get; set; }

        public int SessionTimeoutInSeconds { get; set; }

        public string EndpointUrl { get; set; }

        public string State { get; set; }

        public int StateValue { get; set; }
    }

}
