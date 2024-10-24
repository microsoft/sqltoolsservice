//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class SecurableTypeMetadata
    {
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public PermissionMetadata[]? Permissions { get; set; }
    }
}