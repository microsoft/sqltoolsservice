//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.AvailabilityGroup;

namespace Microsoft.SqlTools.ServiceLayer.AvailabilityGroup.Contracts
{
    /// <summary>
    /// a class for storing various properties of availability groups
    /// </summary>
    public class AvailabilityGroupInfo
    {
        /// <summary>
        /// Gets or sets the name of the availability group
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display name of the cluster type
        /// </summary>
        public string ClusterType { get; set; }

        /// <summary>
        /// Gets or sets the integer value of the clustery type
        /// </summary>
        public int ClusterTypeValue { get; set; }

        /// <summary>
        /// Gets or sets the availability replicas
        /// </summary>
        public AvailabilityReplicaInfo[] Replicas { get; set; }

        /// <summary>
        /// Gets or sets the availability databases
        /// </summary>
        public AvailabilityDatabaseInfo[] Databases { get; set; }

        /// <summary>
        /// Gets or sets the display name of the local replica role
        /// </summary>
        public string LocalReplicaRole { get; set; }

        /// <summary>
        /// Gets or sets the integer value of the local replica role
        /// </summary>
        public int LocalReplicaRoleValue { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the availability group is a basic availability group
        /// </summary>
        public bool BasicAvailabilityGroup { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the database level heath detection is enabled
        /// </summary>
        public bool DatabaseHealthTrigger { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the DTC support is enabled
        /// </summary>
        public bool DtcSupportEnabled { get; set; }

        /// <summary>
        /// Gets or sets the number of required synchronized secondaries to commit
        /// </summary>
        public int RequiredSynchronizedSecondariesToCommit { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the BasicAvailabilityGroup property is enabled
        /// </summary>
        public bool IsSupported_BasicAvailabilityGroup { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the DatabaseHealthTrigger property is enabled
        /// </summary>
        public bool IsSupported_DatabaseHealthTrigger { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the DtcSupportEnabled property is enabled
        /// </summary>
        public bool IsSupported_DtcSupportEnabled { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the RequiredSynchronizedSecondariesToCommit property is enabled
        /// </summary>
        public bool IsSupported_RequiredSynchronizedSecondariesToCommit { get; set; }
    }

}
