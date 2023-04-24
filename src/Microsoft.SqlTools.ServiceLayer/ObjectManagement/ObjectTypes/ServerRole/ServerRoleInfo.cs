//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// a class for storing various server role properties
    /// </summary>
    public class ServerRoleInfo : SqlObject
    {
        public string? Owner { get; set; }
        public string[]? Members { get; set; }
        public string[]? Memberships { get; set; }
        public bool IsFixedRole { get; set; }
    }
}
