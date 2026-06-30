//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Delegate invoked when the host is shutting down.
    /// </summary>
    public delegate Task ShutdownTaskCallback(object shutdownParams, RequestContext<object> shutdownRequestContext);

    /// <summary>
    /// The service-host surface required by the language service: the protocol endpoint
    /// (request/event handler registration plus event sending), the extension service provider,
    /// and shutdown-task registration. The hosting service layer implements this so the language
    /// service has no concrete dependency on the ServiceHost type (inverted control).
    /// </summary>
    public interface ILanguageServiceHost : IProtocolEndpoint
    {
        /// <summary>
        /// The service provider used to resolve and load extensions (e.g. completion extensions).
        /// </summary>
        IMultiServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Registers a callback to run when the host shuts down.
        /// </summary>
        void RegisterShutdownTask(ShutdownTaskCallback callback);
    }
}
