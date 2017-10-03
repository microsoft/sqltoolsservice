//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.SqlClient;
using System.Net;
using System.Text.RegularExpressions;

namespace Microsoft.SqlTools.Azure.Core.FirewallRule
{
    internal interface IFirewallErrorParser
    {
        /// <summary>
        /// Parses given error message and error code to see if it's firewall rule error 
        /// and finds the blocked ip address
        /// </summary>
        FirewallParserResponse ParseErrorMessage(string errorMessage, int errorCode);

        /// <summary>
        /// Parses given error message and error code to see if it's firewall rule error 
        /// and finds the blocked ip address
        /// </summary>      
        FirewallParserResponse ParseException(SqlException sqlException);
    }

    /// <summary>
    /// Parses an error to check for firewall rule error. Will include the blocked ip address if firewall rule error is detected
    /// </summary>
    internal class FirewallErrorParser : IFirewallErrorParser
    {
        /// <summary>
        /// Parses given error message and error code to see if it's firewall rule error 
        /// and finds the blocked ip address
        /// </summary>  
        public FirewallParserResponse ParseException(SqlException sqlException)
        {
            CommonUtil.CheckForNull(sqlException, "sqlException");
            return ParseErrorMessage(sqlException.Message, sqlException.Number);
        }

        /// <summary>
        /// Parses given error message and error code to see if it's firewall rule error 
        /// and finds the blocked ip address
        /// </summary>     
        public FirewallParserResponse ParseErrorMessage(string errorMessage, int errorCode)
        {
            CommonUtil.CheckForNull(errorMessage, "errorMessage");

            FirewallParserResponse response = new FirewallParserResponse();
            if (IsSqlAzureFirewallBlocked(errorCode))
            {
                // Connection failed due to blocked client IP
                IPAddress clientIp;
                if (TryParseClientIp(errorMessage, out clientIp))
                {
                    response = new FirewallParserResponse(true, clientIp);
                }
            }
            return response;
        }

        /// <summary>
        /// Parses the given message to find the blocked ip address
        /// </summary>        
        private static bool TryParseClientIp(string message, out IPAddress clientIp)
        {
            clientIp = null;
            try
            {
                Regex regex =
                    new Regex(
                        @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b",
                        RegexOptions.IgnoreCase);
                Match match = regex.Match(message);

                if (match.Success)
                {
                    string clientIpValue = match.Value;
                    return IPAddress.TryParse(clientIpValue, out clientIp);
                }

                return false;
            }
            catch (Exception)
            {
                //TODO: trace?
                return false;
            }
        }

        /// <summary>
        /// Returns true if given error code is firewall rule blocked error code
        /// </summary>      
        private bool IsSqlAzureFirewallBlocked(int errorCode)
        {
            return errorCode == SqlAzureFirewallBlockedErrorNumber;
        }

        private const int SqlAzureFirewallBlockedErrorNumber = 40615; // http://msdn.microsoft.com/en-us/library/windowsazure/ff394106.aspx
    }
}
