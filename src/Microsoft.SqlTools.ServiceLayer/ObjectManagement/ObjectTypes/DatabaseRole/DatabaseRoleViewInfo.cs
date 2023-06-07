//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// a class for storing various database role view properties
    /// </summary>
    public class DatabaseRoleViewInfo : SecurityPrincipalViewInfo
    {
        public string[]? Schemas { get; set; }
    }
}
