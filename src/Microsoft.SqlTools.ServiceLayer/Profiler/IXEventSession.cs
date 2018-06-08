//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Main class for Profiler Service functionality
    /// </summary>
    public interface IXEventSession
    {
        /// <summary>
        /// Gets unique XEvent session ID
        /// </summary>
        int ID { get; }

        /// <summary>
        /// Starts XEvent session
        /// </summary>
        void Start();

        /// <summary>
        /// Stops XEvent session
        /// </summary>
        void Stop();

        /// <summary>
        /// Reads XEvent XML from the default session target
        /// </summary>
        string GetTargetXml();
    }
}
