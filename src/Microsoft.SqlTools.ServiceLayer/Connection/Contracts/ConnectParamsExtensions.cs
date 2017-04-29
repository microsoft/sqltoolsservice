//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Extension methods to ConnectParams
    /// </summary>
    public static class ConnectParamsExtensions
    {
        /// <summary>
        /// Check that the fields in ConnectParams are all valid
        /// </summary>
        public static bool IsValid(this ConnectParams parameters, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(parameters.OwnerUri))
            {
                errorMessage = SR.ConnectionParamsValidateNullOwnerUri; 
            }
            else if (parameters.Connection == null)
            {
                errorMessage = SR.ConnectionParamsValidateNullConnection;
            }
            else if (!string.IsNullOrEmpty(parameters.Connection.ConnectionString))
            {
                // Do not check other connection parameters if a connection string is present
                return string.IsNullOrEmpty(errorMessage);
            }
            else if (string.IsNullOrEmpty(parameters.Connection.ServerName))
            {
                errorMessage = SR.ConnectionParamsValidateNullServerName;
            }
            else if (string.IsNullOrEmpty(parameters.Connection.AuthenticationType) || parameters.Connection.AuthenticationType == "SqlLogin")
            {
                // For SqlLogin, username/password cannot be empty
                if (string.IsNullOrEmpty(parameters.Connection.UserName))
                {
                    errorMessage = SR.ConnectionParamsValidateNullSqlAuth("UserName");
                }
                else if( string.IsNullOrEmpty(parameters.Connection.Password))
                {
                    errorMessage = SR.ConnectionParamsValidateNullSqlAuth("Password");
                }
            }

            return string.IsNullOrEmpty(errorMessage);
        }
    }
}
