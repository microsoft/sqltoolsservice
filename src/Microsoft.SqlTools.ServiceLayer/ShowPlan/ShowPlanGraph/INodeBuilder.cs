//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph
{
    /// <summary>
    /// Interface represents an abstract builder that gets
    /// data from the data source and represents it as
    /// an array of AnalysisServices Graph objects.
    /// </summary>
    public interface INodeBuilder
	{
        /// <summary>
        /// Builds one or more Graphs that
        /// represnet data from the data source.
        /// </summary>
        /// <param name="dataSource">Data Source.</param>
        /// <returns>An array of AnalysisServices Graph objects.</returns>
		ShowPlanGraph[] Execute(object dataSource);
	}
}
