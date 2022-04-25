//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Class that contains data for a single profile event
    /// </summary>
    public class ProfilerSessionTemplate
    {
        /// <summary>
        /// Initialize a new ProfilerEvent with required parameters
        /// </summary>
        public ProfilerSessionTemplate(string name, string defaultView, string createStatement)
        {
            this.Name = name;
            this.DefaultView = defaultView;
            this.CreateStatement = createStatement;
        }

        /// <summary>
        /// Profiler event name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Profiler event timestamp
        /// </summary>
        public string DefaultView { get; private set; }

        /// <summary>
        /// Profiler event timestamp
        /// </summary>
        public string CreateStatement { get; private set; }

        /// <summary>
        /// Equals method
        /// </summary>
        public bool Equals(ProfilerSessionTemplate t)
        {
            // if parameter is null return false:
            if ((object)t == null)
            {
                return false;
            }

            return this.Name == t.Name
                && this.DefaultView == t.DefaultView
                && this.CreateStatement == t.CreateStatement;
        }
    }
}
