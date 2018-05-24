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
        public string Name { get; set; }

        public string ClusterType { get; set; }

        public int ClusterTypeValue { get; set; }

        public AvailabilityReplicaInfo[] Replicas { get; set; }

        public AvailabilityDatabaseInfo[] Databases { get; set; }

        public string LocalReplicaRole { get; set; }

        public int LocalReplicaRoleValue { get; set; }

        public bool BasicAvailabilityGroup { get; set; }

        public bool DatabaseHealthTrigger { get; set; }

        public bool DtcSupportEnabled { get; set; }

        public int RequiredSynchronizedSecondariesToCommit { get; set; }

        public bool IsSupported_BasicAvailabilityGroup { get; set; }

        public bool IsSupported_DatabaseHealthTrigger { get; set; }

        public bool IsSupported_DtcSupportEnabled { get; set; }

        public bool IsSupported_RequiredSynchronizedSecondariesToCommit { get; set; }
    }

}
