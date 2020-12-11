//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class ModelRequestBase
    {
        /// <summary>
        /// Models database name
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// The schema for model table
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Models table name
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }
    }

    public class ModelResponseBase : ResultStatus
    {
    }
}
