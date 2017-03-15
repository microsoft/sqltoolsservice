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
            serviceHost.SetRequestHandler(ScriptingScriptAsRequest.Type, HandleScriptingScriptAsRequest);
        }

        /// <summary>
        /// Handles script as request messages
        /// </summary>
        /// <param name="scriptingParams"></param>
        /// <param name="requestContext"></param>
        internal static async Task HandleScriptingScriptAsRequest(
            ScriptingScriptAsParams scriptingParams,
            RequestContext<ScriptingScriptAsResult> requestContext)
        {
            string script = string.Empty;
            if (scriptingParams.Operation == ScriptOperation.Select)
            {
                script = string.Format(
@"SELECT *
FROM {0}.{1}",
                scriptingParams.Metadata.Schema, scriptingParams.Metadata.Name);
            }
            else if (scriptingParams.Operation == ScriptOperation.Create)
            {
                script = string.Format(
@"CREATE {0}.{1}",
                scriptingParams.Metadata.Schema, scriptingParams.Metadata.Name);
            }
            else if (scriptingParams.Operation == ScriptOperation.Update)
            {
                script = string.Format(
@"UPDATE {0}.{1}",
                scriptingParams.Metadata.Schema, scriptingParams.Metadata.Name);
            }
            else if (scriptingParams.Operation == ScriptOperation.Insert)
            {
                script = string.Format(
@"INSERT {0}.{1}",
                scriptingParams.Metadata.Schema, scriptingParams.Metadata.Name);
            }
            else if (scriptingParams.Operation == ScriptOperation.Delete)
            {
                script = string.Format(
@"DELETE {0}.{1}",
                scriptingParams.Metadata.Schema, scriptingParams.Metadata.Name);
            }

            await requestContext.SendResult(new ScriptingScriptAsResult()
            {
                OwnerUri = scriptingParams.OwnerUri,
                Script = script
            });
        }
    }
}
