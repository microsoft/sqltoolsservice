//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// The information required to render the user view.
    /// </summary>
    public class UserViewInfo : SecurityPrincipalViewInfo
    {
        public DatabaseUserType[]? UserTypes { get; set; }

        public string[]? Languages { get; set; }

        public string[]? Schemas { get; set; }

        public string[]? Logins { get; set; }

        public string[]? DatabaseRoles { get; set; }
    }
}
