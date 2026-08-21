//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;

namespace Microsoft.SqlTools.Sts2.Drivers.SqlClient
{
    /// <summary>Maps SqlClient failures to stable Sts2.* codes (server-free, unit-testable).</summary>
    public static class SqlClientErrorMapping
    {
        /// <summary>Classifies a connection-open failure.</summary>
        public static string ClassifyOpen(SqlException ex)
        {
            foreach (SqlError error in ex.Errors)
            {
                switch (error.Number)
                {
                    case 18456: // login failed
                    case 18452:
                    case 4060:  // cannot open database (often auth/permission)
                        return Sts2ErrorCodes.ConnectionFailedAuth;
                    case -2:    // timeout
                        return Sts2ErrorCodes.ConnectionFailedTimeout;
                    case 53:    // network path
                    case 40:    // could not open a connection
                        return Sts2ErrorCodes.ConnectionFailedNetwork;
                }
            }
            return ClassifyOpenNumber(ex.Number);
        }

        internal static string ClassifyOpenNumber(int number) => number switch
        {
            18456 or 18452 or 4060 => Sts2ErrorCodes.ConnectionFailedAuth,
            -2 => Sts2ErrorCodes.ConnectionFailedTimeout,
            _ => Sts2ErrorCodes.ConnectionFailedNetwork,
        };

        /// <summary>Classifies provider failures raised while executing or streaming a query.</summary>
        internal static string ClassifyQuery(SqlException ex)
        {
            foreach (SqlError error in ex.Errors)
            {
                if (IsTransportNumber(error.Number))
                {
                    return Sts2ErrorCodes.QueryFailedTransport;
                }
            }
            return ClassifyQueryNumber(ex.Number);
        }

        internal static string ClassifyQueryNumber(int number) => IsTransportNumber(number)
            ? Sts2ErrorCodes.QueryFailedTransport
            : Sts2ErrorCodes.QueryFailedServer;

        private static bool IsTransportNumber(int number) => number is
            -2 or    // command timeout
            20 or    // instance does not support encryption / connection setup
            40 or    // could not open a connection
            53 or    // network path not found
            64 or    // network name unavailable
            121 or   // semaphore timeout
            233 or   // no process on the other end of the pipe
            258 or   // wait timed out
            10053 or // connection aborted
            10054 or // connection reset
            10060 or // connection timed out
            11001;   // host not known

        /// <summary>Builds the server-error detail from the first SqlError, if any.</summary>
        public static ServerErrorDetail? ServerDetail(SqlException ex)
        {
            if (ex.Errors.Count == 0)
            {
                return null;
            }
            SqlError error = ex.Errors[0];
            return new ServerErrorDetail
            {
                Number = error.Number,
                Severity = error.Class,
                State = error.State,
                Line = error.LineNumber > 0 ? error.LineNumber : null,
            };
        }
    }
}
