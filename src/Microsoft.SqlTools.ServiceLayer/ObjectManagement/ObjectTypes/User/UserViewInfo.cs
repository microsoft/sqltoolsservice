//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// The information required to render the user view.
    /// </summary>
    public class UserViewInfo : SqlObjectViewInfo
    {
        public bool SupportContainedUser { get; set; }

        public bool SupportWindowsAuthentication { get; set; }

        public bool SupportAADAuthentication { get; set; }

        public bool SupportSQLAuthentication { get; set; }

        public string[]? Languages { get; set; }

        public string[]? Schemas { get; set; }

        public string[]? Logins { get; set; }

        public string[]? DatabaseRoles { get; set; }
    }
}
