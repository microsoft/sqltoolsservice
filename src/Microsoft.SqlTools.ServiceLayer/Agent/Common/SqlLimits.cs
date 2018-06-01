//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

/// <summary>
/// Defines static values for both Yukon and Shiloh
/// </summary>
namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    #region SQL Limits
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class SqlLimits
    {
        #region Constructors
        internal SqlLimits()
        {
        }
        #endregion
        
        //Define Version 9 Limits
        //Currently MAX is the same for both nVarchar and Varchar.
        public static readonly int VarcharMax = 1073741824;
        public static readonly int SysName = 128;

        //Define Pre-Version 9 Limits
        public static readonly int CommandDimensionMaxLength=3200;
    }
    #endregion
}
