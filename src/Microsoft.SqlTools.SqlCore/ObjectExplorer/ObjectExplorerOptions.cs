//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer
{
    public class ObjectExplorerOptions
    {
        /// <summary>
        /// Function that returns flag to group nodes by schema. Default is false
        /// </summary>
        public Func<bool> GroupBySchemaFlagGetter { get; set; } = () => false;

        /// <summary>
        /// Timeout for OE session operations in seconds. Default is 60 seconds
        /// </summary>
        public int OperationTimeout { get; set; } = 60;
    }
}