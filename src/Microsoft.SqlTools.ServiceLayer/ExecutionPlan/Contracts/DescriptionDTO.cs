//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    /// <summary>
    /// Holds all information about the execution plan's graph.
    /// </summary>
    public class DescriptionDTO
    {
        /// <summary
        /// The title for a graph
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Contains a string representation of the query the execution plan is for.
        /// </summary>
        public string QueryText { get; set; }
        /// <summary>
        /// The enabled clustered mode
        /// </summary>
        public string ClusteredMode { get; set; }
        /// <summary>
        /// Indicates if clustered mode is enabled on the server
        /// </summary>
        public bool IsClusteredMode { get; set; }
        /// <summary>
        /// List of missing indices for the graph
        /// </summary>
        public List<MissingIndex> MissingIndices { get; set; }

        /// <summary>
        /// Instantiates a DescriptionDTO
        /// </summary>
        /// <param name="description">The description object that will be used to create the DTO</param>
        public DescriptionDTO(Description description)
        {
            Title = description.Title;
            QueryText = description.QueryText;
            ClusteredMode = description.ClusteredMode;
            IsClusteredMode = description.IsClusteredMode;
            MissingIndices = description.MissingIndices;
        }
    }
}
