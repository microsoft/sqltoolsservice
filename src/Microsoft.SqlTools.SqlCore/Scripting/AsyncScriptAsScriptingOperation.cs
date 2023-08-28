//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;

namespace Microsoft.SqlTools.SqlCore.Scripting
{
    public class AsyncScriptAsScriptingOperation
    {
        public static async Task<string> GetScriptAsScript(ScriptingParams parameters, AccessToken? accessToken)
        {
            var scriptAsOperation = new ScriptAsScriptingOperation(parameters, accessToken?.Token);
            TaskCompletionSource<string> scriptAsTask = new TaskCompletionSource<string>();

            scriptAsOperation.CompleteNotification += (sender, args) =>
            {
                if (args.HasError)
                {
                    scriptAsTask.SetException(new Exception(args.ErrorMessage));
                }
                scriptAsTask.SetResult(scriptAsOperation.ScriptText);
            };

            scriptAsOperation.ProgressNotification += (sender, args) =>
            {
                if(args.ErrorMessage != null)
                {
                    scriptAsTask.SetException(new Exception(args.ErrorMessage));
                }
            };

            scriptAsOperation.Execute();
            return await scriptAsTask.Task;
        }
    }
}