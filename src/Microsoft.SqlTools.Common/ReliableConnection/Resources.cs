//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    /// <summary>
    /// Contains string resources used throughout ReliableConnection code.
    /// </summary>
    public static class Resources
    {
        internal static string AmbientSettingFormat
        {
            get
            {
                return "{0}: {1}";
            }
        }

        public static string ConnectionPassedToIsCloudShouldBeOpen
        {
            get
            {
                return "connection passed to IsCloud should be open.";
            }
        }

        public static string ConnectionPropertyNotSet
        {
            get
            {
                return "Connection property has not been initialized.";
            }
        }

        public static string ExceptionCannotBeRetried
        {
            get
            {
                return "Exception cannot be retried because of err #{0}:{1}";
            }
        }

        public static string ErrorParsingConnectionString
        {
            get
            {
                return "Error parsing connection string {0}";
            }
        }

        public static string FailedToCacheIsCloud
        {
            get
            {
                return "failed to cache the server property of IsAzure";
            }
        }

        public static string FailedToParseConnectionString
        {
            get
            {
                return "failed to parse the provided connection string: {0}";
            }
        }

        public static string IgnoreOnException
        {
            get
            {
                return "Retry number {0}. Ignoring Exception: {1}";
            }
        }

        public static string InvalidCommandType
        {
            get
            {
                return "Unsupported command object.  Use SqlCommand or ReliableSqlCommand.";
            }
        }

        public static string InvalidConnectionType
        {
            get
            {
                return "Unsupported connection object.  Use SqlConnection or ReliableSqlConnection.";
            }
        }

        public static string LoggingAmbientSettings
        {
            get
            {
                return "Logging Ambient Settings...";
            }
        }

        internal static string Mode
        {
            get
            {
                return "Mode";
            }
        }

        public static string OnlyReliableConnectionSupported
        {
            get
            {
                return "Connection property can only be set to a value of type ReliableSqlConnection.";
            }
        }

        internal static string RetryOnException
        {
            get
            {
                return "Retry number {0}. Delaying {1} ms before next retry. Exception: {2}";
            }
        }

        internal static string ThrottlingTypeInfo
        {
            get
            {
                return "ThrottlingTypeInfo";
            }
        }

        public static string UnableToAssignValue
        {
            get
            {
                return "Unable to assign the value of type {0} to {1}";
            }
        }

        public static string UnableToRetrieveAzureSessionId
        {
            get
            {
                return "Unable to retrieve Azure session-id.";
            }
        }

        internal static string ServerInfoCacheMiss
        {
            get
            {
                return "Server Info does not have the requested property in the cache";
            }
        }
    }
}
