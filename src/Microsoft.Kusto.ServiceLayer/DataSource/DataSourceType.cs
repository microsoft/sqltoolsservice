//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <summary>
    /// Represents the type of a data source.
    /// </summary>
    public enum DataSourceType
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        None,

        /// <summary>
        /// A Kusto cluster.
        /// </summary>
        Kusto,

        /// <summary>
        /// An Application Insights subscription.
        /// </summary>
        ApplicationInsights,

        /// <summary>
        /// An Operations Management Suite (OMS) Log Analytics workspace.
        /// </summary>
        LogAnalytics
    }
}