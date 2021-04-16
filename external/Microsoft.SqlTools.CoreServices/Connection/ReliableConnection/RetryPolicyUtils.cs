//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.SqlTools.Hosting.Utility;

namespace Microsoft.SqlTools.CoreServices.Connection.ReliableConnection
{
    internal static class RetryPolicyUtils
    {
        /// <summary>
        /// Approved list of transient errors that should be retryable during Network connection stages
        /// </summary>
        private static readonly HashSet<int> _retryableNetworkConnectivityErrors;
        /// <summary>
        /// Approved list of transient errors that should be retryable on Azure
        /// </summary>
        private static readonly HashSet<int> _retryableAzureErrors;
        /// <summary>
        /// Blocklist of non-transient errors that should stop retry during data transfer operations
        /// </summary>
        private static readonly HashSet<int> _nonRetryableDataTransferErrors;

        static RetryPolicyUtils()
        {
            _retryableNetworkConnectivityErrors = new HashSet<int>
            {
                /// A severe error occurred on the current command.  The results, if any, should be discarded.
                0,

                //// DBNETLIB Error Code: 20
                //// The instance of SQL Server you attempted to connect to does not support encryption.
                (int) ProcessNetLibErrorCode.EncryptionNotSupported,

                //// DBNETLIB Error Code: -2
                //// Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding.
                (int)ProcessNetLibErrorCode.Timeout,

                //// SQL Error Code: 64
                //// A connection was successfully established with the server, but then an error occurred during the login process. 
                //// (provider: TCP Provider, error: 0 - The specified network name is no longer available.) 
                64,
                
                //// SQL Error Code: 233
                //// The client was unable to establish a connection because of an error during connection initialization process before login. 
                //// Possible causes include the following: the client tried to connect to an unsupported version of SQL Server; the server was too busy 
                //// to accept new connections; or there was a resource limitation (insufficient memory or maximum allowed connections) on the server. 
                //// (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
                233,

                //// SQL Error Code: 10053
                //// A transport-level error has occurred when receiving results from the server.
                //// An established connection was aborted by the software in your host machine.
                10053,

                //// SQL Error Code: 10054
                //// A transport-level error has occurred when sending the request to the server. 
                //// (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
                10054,

                //// SQL Error Code: 10060
                //// A network-related or instance-specific error occurred while establishing a connection to SQL Server. 
                //// The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server 
                //// is configured to allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed 
                //// because the connected party did not properly respond after a period of time, or established connection failed 
                //// because connected host has failed to respond.)"}
                10060,

                // SQL Error Code: 11001
                // A network-related or instance-specific error occurred while establishing a connection to SQL Server. 
                // The server was not found or was not accessible. Verify that the instance name is correct and that SQL 
                // Server is configured to allow remote connections. (provider: TCP Provider, error: 0 - No such host is known.)
                11001,

                //// SQL Error Code: 40613
                //// Database XXXX on server YYYY is not currently available. Please retry the connection later. If the problem persists, contact customer 
                //// support, and provide them the session tracing ID of ZZZZZ.
                40613,                
            };

            _retryableAzureErrors = new HashSet<int>
                                        {
                //// SQL Error Code: 40
                //// Could not open a connection to SQL Server
                //// (provider: Named Pipes Provider, error: 40 Could not open a connection to SQL Server)
                40,
                
                //// SQL Error Code: 121
                //// A transport-level error has occurred when receiving results from the server. 
                //// (provider: TCP Provider, error: 0 - The semaphore timeout period has expired.)
                121,
                
                //// SQL Error Code: 913 (noticed intermittently on SNAP runs with connected unit tests)
                //// Could not find database ID %d. Database may not be activated yet or may be in transition. Reissue the query once the database is available.
                //// If you do not think this error is due to a database that is transitioning its state and this error continues to occur, contact your primary support provider.
                //// Please have available for review the Microsoft SQL Server error log and any additional information relevant to the circumstances when the error occurred.
                913,

                //// SQL Error Code: 1205
                //// Transaction (Process ID %d) was deadlocked on %.*ls resources with another process and has been chosen as the deadlock victim. Rerun the transaction.
                1205,

                //// SQL Error Code: 40501
                //// The service is currently busy. Retry the request after 10 seconds. Code: (reason code to be decoded).
                RetryPolicy.ThrottlingReason.ThrottlingErrorNumber,
                
                //// SQL Error Code: 10928
                //// Resource ID: %d. The %s limit for the database is %d and has been reached.
                10928,

                //// SQL Error Code: 10929
                //// Resource ID: %d. The %s minimum guarantee is %d, maximum limit is %d and the current usage for the database is %d.
                //// However, the server is currently too busy to support requests greater than %d for this database.
                10929,

                //// SQL Error Code: 40143
                //// The service has encountered an error processing your request. Please try again.
                40143,

                //// SQL Error Code: 40197
                //// The service has encountered an error processing your request. Please try again.
                40197,

                //// Sql Error Code: 40549 (not supposed to be used anymore as of Q2 2011)
                //// Session is terminated because you have a long-running transaction. Try shortening your transaction.
                40549,

                //// Sql Error Code: 40550 (not supposed to be used anymore as of Q2 2011)
                //// The session has been terminated because it has acquired too many locks. Try reading or modifying fewer rows in a single transaction.
                40550,

                //// Sql Error Code: 40551 (not supposed to be used anymore as of Q2 2011)
                //// The session has been terminated because of excessive TEMPDB usage. Try modifying your query to reduce the temporary table space usage.
                40551,

                //// Sql Error Code: 40552 (not supposed to be used anymore as of Q2 2011)
                //// The session has been terminated because of excessive transaction log space usage. Try modifying fewer rows in a single transaction.
                40552,

                //// Sql Error Code: 40553 (not supposed to be used anymore as of Q2 2011)
                //// The session has been terminated because of excessive memory usage. Try modifying your query to process fewer rows.
                40553,
                
                //// SQL Error Code: 40627
                //// Operation on server YYY and database XXX is in progress.  Please wait a few minutes before trying again.
                40627,

                //// SQL Error Code: 40671 (DB CRUD)
                //// Unable to '%.*ls' '%.*ls' on server '%.*ls'. Please retry the connection later.
                40671,

                //// SQL Error Code: 40676 (DB CRUD)
                //// '%.*ls' request was received but may not be processed completely at this time, 
                //// please query the sys.dm_operation_status table in the master database for status.
                40676,

                //// SQL Error Code: 45133
                //// A connection failed while the operation was still in progress, and the outcome of the operation is unknown.
                45133,
            };

            foreach(int errorNum in _retryableNetworkConnectivityErrors)
            {
                _retryableAzureErrors.Add(errorNum);
            }

            _nonRetryableDataTransferErrors = new HashSet<int>
                                                  {
                //// Syntax error
                156,
                
                //// Cannot insert duplicate key row in object '%.*ls' with unique index '%.*ls'. The duplicate key value is %ls.
                2601,

                //// Violation of %ls constraint '%.*ls'. Cannot insert duplicate key in object '%.*ls'. The duplicate key value is %ls.
                2627,
                
                //// Cannot find index '%.*ls'.
                2727, 
                
                //// SqlClr stack error
                6522,

                //// Divide by zero error encountered.
                8134,

                //// Could not repair this error.
                8922,

                //// Bug 1110540: This error means the table is corrupted due to hardware failure, so we do not want to retry.
                //// Table error: Object ID %d. The text, ntext, or image node at page %S_PGID, slot %d, text ID %I64d is referenced by page %S_PGID, slot %d, but was not seen in the scan.
                8965,

                //// The query processor is unable to produce a plan because the clustered index is disabled.
                8655,
                
                //// The query processor is unable to produce a plan because table is unavailable because the heap is corrupted
                8674,

                //// SqlClr permission / load error. 
                //// Example Message: An error occurred in the Microsoft .NET Framework while trying to load assembly 
                10314,

                //// '%ls' is not supported in this version of SQL Server.
                40514,

                //// The database 'XYZ' has reached its size quota. Partition or delete data, drop indexes, or consult the documentation for possible resolutions
                40544,
            };
        }

        public static bool IsRetryableNetworkConnectivityError(int errorNumber)
        {
            // .NET core has a bug on OSX/Linux that makes this error number always zero (issue 12472)
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return errorNumber != 0 && _retryableNetworkConnectivityErrors.Contains(errorNumber);
            }
            return _retryableNetworkConnectivityErrors.Contains(errorNumber);
        }

        public static bool IsRetryableAzureError(int errorNumber)
        {
            return _retryableAzureErrors.Contains(errorNumber) || _retryableNetworkConnectivityErrors.Contains(errorNumber);
        }

        public static bool IsNonRetryableDataTransferError(int errorNumber)
        {
            return _nonRetryableDataTransferErrors.Contains(errorNumber);
        }

        public static void AppendThrottlingDataIfIsThrottlingError(SqlException sqlException, SqlError error)
        {
            //// SQL Error Code: 40501
            //// The service is currently busy. Retry the request after 10 seconds. Code: (reason code to be decoded).
            if(error.Number ==  RetryPolicy.ThrottlingReason.ThrottlingErrorNumber)
            {
                // Decode the reason code from the error message to determine the grounds for throttling.
                var condition = RetryPolicy.ThrottlingReason.FromError(error);

                // Attach the decoded values as additional attributes to the original SQL exception.
                sqlException.Data[condition.ThrottlingMode.GetType().Name] = condition.ThrottlingMode.ToString();
                sqlException.Data[condition.GetType().Name] = condition;
            }
        }
        /// <summary>
        /// Calculates the length of time to delay a retry based on the number of retries up to this point.
        /// As the number of retries increases, the timeout increases exponentially based on the intervalFactor.
        /// Uses default values for the intervalFactor (<see cref="RetryPolicyDefaults.DefaultBackoffIntervalFactor"/>), minInterval
        /// (<see cref="RetryPolicyDefaults.DefaultSchemaMinInterval"/>) and maxInterval (<see cref="RetryPolicyDefaults.DefaultMaxRetryInterval"/>)
        /// </summary>
        /// <param name="currentRetryCount">Total number of retries including the current retry</param>
        /// <returns>TimeSpan defining the length of time to delay</returns>
        internal static TimeSpan CalcExponentialRetryDelayWithSchemaDefaults(int currentRetryCount)
        {
            return CalcExponentialRetryDelay(currentRetryCount,
                RetryPolicyDefaults.DefaultBackoffIntervalFactor,
                RetryPolicyDefaults.DefaultSchemaMinInterval,
                RetryPolicyDefaults.DefaultMaxRetryInterval);
        }

        /// <summary>
        /// Calculates the length of time to delay a retry based on the number of retries up to this point.
        /// As the number of retries increases, the timeout increases exponentially based on the intervalFactor.
        /// A very large retry count can cause huge delay, so the maxInterval is used to cap delay time at a sensible
        /// upper bound
        /// </summary>
        /// <param name="currentRetryCount">Total number of retries including the current retry</param>
        /// <param name="intervalFactor">Controls the speed at which the delay increases - the retryCount is raised to this power as
        /// part of the function </param>
        /// <param name="minInterval">Minimum interval between retries. The basis for all backoff calculations</param>
        /// <param name="maxInterval">Maximum interval between retries. Backoff will not take longer than this period.</param>
        /// <returns>TimeSpan defining the length of time to delay</returns>
        internal static TimeSpan CalcExponentialRetryDelay(int currentRetryCount, double intervalFactor, TimeSpan minInterval, TimeSpan maxInterval)
        {
            try
            {
                return checked(TimeSpan.FromMilliseconds(
                    Math.Max(
                        Math.Min(
                            Math.Pow(intervalFactor, currentRetryCount - 1) * minInterval.TotalMilliseconds,
                            maxInterval.TotalMilliseconds
                        ),
                        minInterval.TotalMilliseconds)
                    ));
            }
            catch (OverflowException)
            {
                // If numbers are too large, could conceivably overflow the double.
                // Since the maxInterval is the largest TimeSpan expected, can safely return this here
                return maxInterval;
            }
        }

        internal static void RaiseAmbientRetryMessage(RetryState retryState, int errorCode)
        {
            Action<SqlServerRetryError> retryMsgHandler = AmbientSettings.ConnectionRetryMessageHandler;
            if (retryMsgHandler != null)
            {
                string msg = SqlServerRetryError.FormatRetryMessage(
                    retryState.RetryCount,
                    retryState.Delay,
                    retryState.LastError);

                retryMsgHandler(new SqlServerRetryError(
                                    msg,
                                    retryState.LastError,
                                    retryState.RetryCount,
                                    errorCode,
                                    ErrorSeverity.Warning));
            }
        }

        internal static void RaiseAmbientIgnoreMessage(RetryState retryState, int errorCode)
        {
            Action<SqlServerRetryError> retryMsgHandler = AmbientSettings.ConnectionRetryMessageHandler;
            if (retryMsgHandler != null)
            {
                string msg = SqlServerRetryError.FormatIgnoreMessage(
                    retryState.RetryCount,
                    retryState.LastError);

                retryMsgHandler(new SqlServerRetryError(
                                    msg,
                                    retryState.LastError,
                                    retryState.RetryCount,
                                    errorCode,
                                    ErrorSeverity.Warning));
            }
        }

        /// <summary>
        /// Traces the Schema retry information before raising the retry message
        /// </summary>
        /// <param name="retryState"></param>
        /// <param name="errorCode"></param>
        /// <param name="azureSessionId"></param>
        internal static void RaiseSchemaAmbientRetryMessage(RetryState retryState, int errorCode, Guid azureSessionId)
        {
            if (azureSessionId != Guid.Empty)
            {
                Logger.Write(TraceEventType.Warning, string.Format(
                    "Retry occurred: session: {0}; attempt - {1}; delay - {2}; exception - \"{3}\"",
                    azureSessionId,
                    retryState.RetryCount,
                    retryState.Delay,
                    retryState.LastError
                 ));

                RaiseAmbientRetryMessage(retryState, errorCode);
            }
        }

        #region ProcessNetLibErrorCode enumeration

        /// <summary>
        /// Error codes reported by the DBNETLIB module.
        /// </summary>
        internal enum ProcessNetLibErrorCode
        {
            /// <summary>
            /// Zero bytes were returned
            /// </summary>
            ZeroBytes = -3,

            /// <summary>
            /// Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding.
            /// </summary>
            Timeout = -2,

            /// <summary>
            /// An unknown net lib error
            /// </summary>
            Unknown = -1,

            /// <summary>
            /// Out of memory
            /// </summary>
            InsufficientMemory = 1,

            /// <summary>
            /// User or machine level access denied
            /// </summary>
            AccessDenied = 2,

            /// <summary>
            /// Connection was already busy processing another request
            /// </summary>
            ConnectionBusy = 3,

            /// <summary>
            /// The connection was broken without a proper disconnect
            /// </summary>
            ConnectionBroken = 4,

            /// <summary>
            /// The connection has reached a limit
            /// </summary>
            ConnectionLimit = 5,

            /// <summary>
            /// Name resolution failed for the given server name
            /// </summary>
            ServerNotFound = 6,

            /// <summary>
            /// Network transport could not be found
            /// </summary>
            NetworkNotFound = 7,

            /// <summary>
            /// A resource required could not be allocated
            /// </summary>
            InsufficientResources = 8,

            /// <summary>
            /// Network stack denied the request as too busy
            /// </summary>
            NetworkBusy = 9,

            /// <summary>
            /// Unable to access the requested network
            /// </summary>
            NetworkAccessDenied = 10,

            /// <summary>
            /// Internal error
            /// </summary>
            GeneralError = 11,

            /// <summary>
            /// The network mode was set incorrectly
            /// </summary>
            IncorrectMode = 12,

            /// <summary>
            /// The given name was not found
            /// </summary>
            NameNotFound = 13,

            /// <summary>
            /// Connection was invalid
            /// </summary>
            InvalidConnection = 14,

            /// <summary>
            /// A read or write error occurred
            /// </summary>
            ReadWriteError = 15,

            /// <summary>
            /// Unable to allocate an additional handle
            /// </summary>
            TooManyHandles = 16,

            /// <summary>
            /// The server reported an error
            /// </summary>
            ServerError = 17,

            /// <summary>
            /// SSL failed
            /// </summary>
            SSLError = 18,

            /// <summary>
            /// Encryption failed with an error
            /// </summary>
            EncryptionError = 19,

            /// <summary>
            /// Remote endpoint does not support encryption
            /// </summary>
            EncryptionNotSupported = 20
        }

        #endregion

    }
}
