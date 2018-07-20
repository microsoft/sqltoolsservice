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
        ///<summary>
        /// Gets or sets the name of the availability database
        ///</summary>
        public string Name { get; set; }

        ///<summary>
        /// Gets or sets the state of the availability database
        ///</summary>
        public string State { get; set; }

        ///<summary>
        /// Gets or sets the integer value of the availability database state
        ///</summary>
        public int StateValue { get; set; }

        ///<summary>
        /// Gets or sets the boolean value indicating whether the data movement is suspended for the availability database
        ///</summary>
        public bool IsSuspended { get; set; }

        ///<summary>
        /// Gets or sets the boolean value indicating whether the database has joined the availability database
        ///</summary>
        public bool IsJoined { get; set; }
    }

}
