//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Main class for Scripting Service functionality
    /// </summary>
    public sealed class ScriptingService
    {
        private static readonly Lazy<ScriptingService> LazyInstance = new Lazy<ScriptingService>(() => new ScriptingService());

        public static ScriptingService Instance => LazyInstance.Value;

        /// <summary>
        /// Initializes the Scripting Service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ScriptingSelectRequest.Type, HandleScriptingSelectRequest);
        }

        internal static async Task HandleScriptingSelectRequest(
            ScriptingSelectParams scriptingParams,
            RequestContext<ScriptingSelectResult> requestContext)
        {
            await requestContext.SendResult(new ScriptingSelectResult());
        }
    }
}
