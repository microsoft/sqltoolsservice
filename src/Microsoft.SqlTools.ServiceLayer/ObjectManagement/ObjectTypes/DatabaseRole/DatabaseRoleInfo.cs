//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// a class for storing various database role properties
    /// </summary>
    public class DatabaseRoleInfo : SecurityPrincipalObject
    {
        public string? Owner { get; set; }
        public string[]? OwnedSchemas { get; set; }
        public ExtendedPropertyInfo[]? ExtendedProperties { get; set; }
        public string[]? Members { get; set; }
    }
}
