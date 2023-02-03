//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{
    public class LoginViewInfo
    {

        public LoginInfo Login { get; set; }
        public bool SupportWindowsAuthentication { get; set; }
        public bool SupportAADAuthentication { get; set; }
        public bool SupportSQLAuthentication { get; set; }
        public bool CanEditLockedOutState { get; set; }
        public string[] Databases;
        public string[] Languages;
        public string[] ServerRoles;
    }
}