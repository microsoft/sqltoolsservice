//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class SecurablePermissionItem
    {
        public string? Permission { get; set; }
        public string? Grantor { get; set; }
        public bool Grant { get; set; }
        public bool WithGrant { get; set; }
    }

    public class SecurablePermissions
    {
        public string? Name { get; set; }
        public string? Schema { get; set; }
        public string? Type { get; set; }
        public string[]? EffectivePermissions { get; set; }
        public SecurablePermissionItem[]? Permissions { get; set; }
    }
}