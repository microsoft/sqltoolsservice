//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// a class for storing various application role properties
    /// </summary>
    public class AppRoleInfo : SecurityPrincipalObject
    {
        public string? DefaultSchema { get; set; }
        public string? Password { get; set; }
        public string[]? OwnedSchemas { get; set; }
        public ExtendedPropertyInfo[]? ExtendedProperties { get; set; }
    }
}
