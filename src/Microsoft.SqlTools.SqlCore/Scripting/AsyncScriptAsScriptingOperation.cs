//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;

namespace Microsoft.SqlTools.SqlCore.Scripting
{
    public class AsyncScriptAsScriptingOperation
    {
        public static async Task<string> GetScriptAsScript(ScriptingParams parameters)
        {
            var scriptAsOperation = new ScriptAsScriptingOperation(parameters, string.Empty);
            return await ExecuteScriptAs(scriptAsOperation);
        }

        /// <summary>
        /// Gets the script as script like select, insert, update, drop and create for the given scripting parameters.
        /// </summary>
        /// <param name="parameters">scripting parameters that contains the object to script and the scripting options</param>
        /// <param name="serverConnection">server connection to use for scripting</param>
        /// <returns>script as script</returns>
        public static async Task<string> GetScriptAsScript(ScriptingParams parameters, ServerConnection? serverConnection)
        {
            var scriptAsOperation = new ScriptAsScriptingOperation(parameters, serverConnection);
            return await ExecuteScriptAs(scriptAsOperation);
        }

        private static async Task<string> ExecuteScriptAs(ScriptAsScriptingOperation scriptAsOperation)
        {
            TaskCompletionSource<string> scriptAsTask = new TaskCompletionSource<string>();
            scriptAsOperation.CompleteNotification += (sender, args) =>
            {
                if (args.HasError)
                {
                    scriptAsTask.SetException(new Exception(args.ErrorMessage));
                }
                else
                {
                    scriptAsTask.SetResult(scriptAsOperation.ScriptText);

                }
            };

            scriptAsOperation.ProgressNotification += (sender, args) =>
            {
                if (args.ErrorMessage != null)
                {
                    scriptAsTask.SetException(new Exception(args.ErrorMessage));
                }
            };

            scriptAsOperation.Execute();
            return await scriptAsTask.Task;
        }
    }
}