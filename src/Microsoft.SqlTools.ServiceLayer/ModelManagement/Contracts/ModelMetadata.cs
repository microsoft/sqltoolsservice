//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement
{
    /// <summary>
    /// Model metadata
    /// </summary>
    public class ModelMetadata
    {
        /// <summary>
        /// Model id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Model content length
        /// </summary>
        public Int64 ContentLength { get; set; }

        /// <summary>
        /// Model name
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Model created date
        /// </summary>
        public string Created { get; set; }

        /// <summary>
        /// Model deployment time
        /// </summary>
        public string DeploymentTime { get; set; }

        /// <summary>
        /// Model version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Model description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Model file path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Model framework
        /// </summary>
        public string Framework { get; set; }

        /// <summary>
        /// Model framework version
        /// </summary>
        public string FrameworkVersion { get; set; }

        /// <summary>
        /// Model run id
        /// </summary>
        public string RunId { get; set; }

        /// <summary>
        /// Model deploy by
        /// </summary>
        public string DeployedBy { get; set; }
    }
}
