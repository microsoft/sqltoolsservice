//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// a class for storing various application role view properties
    /// </summary>
    public class AppRoleViewInfo : SecurityPrincipalViewInfo
    {
        public string[]? Schemas { get; set; }
    }
}
