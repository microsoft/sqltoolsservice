//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// a class for storing various server role view properties
    /// </summary>
    public class ServerRoleViewInfo : SecurityPrincipalViewInfo
    {
        public bool IsFixedRole { get; set; }
        public string[]? ServerRoles { get; set; }
    }
}
