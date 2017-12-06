//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Scripting Operation type
    /// </summary>
    public enum ScriptingOperationType
    {
        Select = 0,
        Create = 1,
        Insert = 2,
        Update = 3,
        Delete = 4,
        Execute = 5,
        Alter = 6
    }
}
