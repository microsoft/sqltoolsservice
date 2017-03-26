//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    public abstract class ScriptingOperation : IDisposable
    {
        public string OperationId { get; protected set; }

        public abstract Task Execute();

        public abstract void Cancel();

        public abstract void Dispose();
    }
}
