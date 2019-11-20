//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.Kusto.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Class that contains data for a single profile event
    /// </summary>
    public class ProfilerEvent
    {
        /// <summary>
        /// Initialize a new ProfilerEvent with required parameters
        /// </summary>
        public ProfilerEvent(string name, string timestamp)
        {
            this.Name = name;
            this.Timestamp = timestamp;
            this.Values = new Dictionary<string, string>();
        }

        /// <summary>
        /// Profiler event name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Profiler event timestamp
        /// </summary>
        public string Timestamp { get; private set; }

        /// <summary>
        /// Profiler event values collection
        /// </summary>
        public Dictionary<string, string> Values { get; private set; }

        /// <summary>
        /// Equals method
        /// </summary>
        public bool Equals(ProfilerEvent p)
        {
            // if parameter is null return false:
            if ((object)p == null)
            {
                return false;
            }

            return this.Name == p.Name
                && this.Timestamp == p.Timestamp
                && this.Values.Count == p.Values.Count;
        }

        /// <summary>
        /// GetHashCode method
        /// </summary>
        public override int GetHashCode()
        {
            int hashCode = this.GetType().ToString().GetHashCode();

            if (this.Name != null)
            {
                hashCode ^= this.Name.GetHashCode();
            }

            if (this.Timestamp != null)
            {
                hashCode ^= this.Timestamp.GetHashCode();
            }

            hashCode ^= this.Values.Count.GetHashCode();

            return hashCode;
        }
    }
}
