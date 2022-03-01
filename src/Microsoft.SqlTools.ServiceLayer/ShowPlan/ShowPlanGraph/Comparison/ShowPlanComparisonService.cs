//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using System;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    /// <summary>
    /// Main class for Show Plan Comparison functionality.
    /// </summary>
    public sealed class ShowPlanComparisonService : IDisposable
    {
        private static readonly Lazy<ShowPlanComparisonService> instance = new(() => new ShowPlanComparisonService());

        private bool disposed;

        /// <summary>
        /// Constructs a new ShowPlanComparison instance with default parameters.
        /// </summary>
        public ShowPlanComparisonService() { }

        /// <summary>
        /// Gets singleton instance object.
        /// </summary>
        public static ShowPlanComparisonService Instance => instance.Value;

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }
        
        /// <summary>
        /// Initializes the ShowPlanComparison service.
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            ServiceHost = serviceHost;
        }

        /// <summary>
        /// Disposes the ShowPlanComparison service.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }
    }
}
