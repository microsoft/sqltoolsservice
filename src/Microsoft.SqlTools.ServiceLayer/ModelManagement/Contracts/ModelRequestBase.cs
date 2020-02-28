//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class ModelRequestBaseParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Model name
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Model name
        /// </summary>
        public string TableName { get; set; }
    }
}
