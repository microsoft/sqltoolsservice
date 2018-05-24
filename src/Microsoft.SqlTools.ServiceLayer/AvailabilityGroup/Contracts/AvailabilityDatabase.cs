//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.AvailabilityGroup;

namespace Microsoft.SqlTools.ServiceLayer.AvailabilityGroup.Contracts
{
    /// <summary>
    /// a class for storing various properties of availability databases
    /// </summary>
    public class AvailabilityDatabaseInfo
    {
        public string Name { get; set; }

        public string State { get; set; }

        public int StateValue { get; set; }

        public bool IsSuspended { get; set; }

        public bool IsJoined { get; set; }
    }

}
