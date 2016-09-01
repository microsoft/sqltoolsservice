//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

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
                errorMessage = "Error: OwnerUri cannot be null or empty."; 
            }
            else if (parameters.Connection == null)
            {
                errorMessage = "Error: Connection details object cannot be null.";
            }
            else if (string.IsNullOrEmpty(parameters.Connection.ServerName))
            {
                errorMessage = "Error: ServerName cannot be null or empty.";
            }
            else if (string.IsNullOrEmpty(parameters.Connection.AuthenticationType) || parameters.Connection.AuthenticationType == "SqlLogin")
            {
                // For SqlLogin, username/password cannot be empty
                if (string.IsNullOrEmpty(parameters.Connection.UserName))
                {
                    errorMessage = "Error: UserName cannot be null or empty when using SqlLogin authentication.";
                }
                else if( string.IsNullOrEmpty(parameters.Connection.Password))
                {
                    errorMessage = "Error: Password cannot be null or empty when using SqlLogin authentication.";
                }
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
