//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    /// <summary>
    /// Registered Model
    /// </summary>
    public class RegisteredModel
    {

        /// <summary>
        /// Model Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Language Owner
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Created Date
        /// </summary>
        public string CreatedDate { get; set; }

        /// <summary>
        /// Model Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Model Id
        /// </summary>
        public string Description { get; set; }

        public string FilePath { get; set; }
    }
}
