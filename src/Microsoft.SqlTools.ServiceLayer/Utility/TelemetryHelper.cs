//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    /// <summary>
    /// Provides common wrappers and helper methods for telemetry.
    /// </summary>
    public static class TelemetryHelper
    {
        /// <summary>
        /// Wrapper for ServiceHost.Instance.SendEvent that ignores errors and 
        /// writes them to the log instead.
        /// </summary>
        /// <typeparam name="TParams">The event parameter type.</typeparam>
        /// <param name="eventType">The type of event being sent.</param>
        /// <param name="eventParams">The event parameters being sent.</param>
        /// <returns>A Task that tracks completion of the send operation, or null
        /// if SendEvent threw an exception.</returns>
        public static Task SendEventAndIgnoreErrors<TParams>(
            EventType<TParams> eventType,
            TParams eventParams)
        {
            try
            {
                return ServiceHost.Instance.SendEvent(eventType, eventParams);
            }
            catch (Exception ex)
            {
                Logger.Write(LogLevel.Verbose, "Exeception in SendEvent " + ex.ToString());
            }
            return null;
        }
    }
}
