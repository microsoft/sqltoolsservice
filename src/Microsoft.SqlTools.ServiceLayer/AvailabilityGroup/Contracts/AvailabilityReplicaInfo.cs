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
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the role display name
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the integer value of the role
        /// </summary>
        public int RoleValue { get; set; }

        /// <summary>
        /// Gets or sets the availability mode display name
        /// </summary>
        public string AvailabilityMode { get; set; }

        /// <summary>
        /// Gets or sets the integer value of the availability mode
        /// </summary>
        public int AvailabilityModeValue { get; set; }

        /// <summary>
        /// Gets or sets the failover mode display name
        /// </summary>
        public string FailoverMode { get; set; }

        /// <summary>
        /// Gets or sets the integer value of the failover mode
        /// </summary>
        public int FailoverModeValue { get; set; }

        /// <summary>
        /// Gets or sets the display name of the connection mode when the replica is in primary role
        /// </summary>
        public string ConnectionsInPrimaryRole { get; set; }

        /// <summary>
        /// Gets or sets the integer value of the ConnectionsInPrimaryRole property
        /// </summary>
        public int ConnectionsInPrimaryRoleValue { get; set; }

        /// <summary>
        /// Gets or sets the display name of the allowed connections when the replica is in secondary role
        /// </summary>
        public string ReadableSecondary { get; set; }

        /// <summary>
        /// Gets or sets the integer value of the ReadableSecondary property
        /// </summary>
        public int ReadableSecondaryValue { get; set; }

        /// <summary>
        /// Gets or sets the display name of the seeding mode
        /// </summary>
        public string SeedingMode { get; set; }

        /// <summary>
        /// Gets or sets the integer value of SeedingMode property
        /// </summary>
        public int SeedingModeValue { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the SeedingMode property is supported
        /// </summary>
        public bool IsSupported_SeedingMode { get; set; }

        /// <summary>
        /// Gets or sets the session timeout value in seconds
        /// </summary>
        public int SessionTimeoutInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the endpoint Url
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the state display name
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the integer value of the State property
        /// </summary>
        public int StateValue { get; set; }
    }

}
