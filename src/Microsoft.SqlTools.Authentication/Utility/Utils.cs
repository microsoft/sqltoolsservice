//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Net.Mail;
using Microsoft.Identity.Client;
using SqlToolsLogger = Microsoft.SqlTools.Utility.Logger;

namespace Microsoft.SqlTools.Authentication.Utility
{
    internal sealed class Utils
    {
        /// <summary>
        /// Validates provided <paramref name="userEmail"/> follows email format.
        /// </summary>
        /// <param name="useremail">Email address</param>
        /// <returns>Whether email is in correct format.</returns>
        public static bool isValidEmail(string userEmail)
        {
            try
            {
                new MailAddress(userEmail);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Log callback handler used for MSAL Client applications.
        /// </summary>
        /// <param name="logLevel">Log level</param>
        /// <param name="message">Log message</param>
        /// <param name="pii">Whether message contains PII information.</param>
        public static void MSALLogCallback(LogLevel logLevel, string message, bool pii)
        {
            switch (logLevel)
            {
                case LogLevel.Error:
                    if (pii) SqlToolsLogger.Pii(message);
                    else SqlToolsLogger.Error(message);
                    break;
                case LogLevel.Warning:
                    if (pii) SqlToolsLogger.Pii(message);
                    else SqlToolsLogger.Warning(message);
                    break;
                case LogLevel.Info:
                    if (pii) SqlToolsLogger.Pii(message);
                    else SqlToolsLogger.Information(message);
                    break;
                case LogLevel.Verbose:
                    if (pii) SqlToolsLogger.Pii(message);
                    else SqlToolsLogger.Verbose(message);
                    break;
                case LogLevel.Always:
                    if (pii) SqlToolsLogger.Pii(message);
                    else SqlToolsLogger.Critical(message);
                    break;
            }
        }
    }
}
