//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.BatchParser.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    public static class SqlRetryProviders
    {
        /// <summary>
        /// Approved list of transient errors that require additional time to wait before connecting again.
        /// </summary>
        private static readonly HashSet<int> _retryableServerlessConnectivityError;
        
        /// <summary>
        /// Max intervals between retries in seconds to wake up serverless instances.
        /// </summary>
        private const int _serverlessMaxIntervalTime = 30;
        
        /// <summary>
        /// Maximum number of retries to wake up serverless instances.
        /// </summary>
        private const int _serverlessMaxRetries = 4;

        static SqlRetryProviders()
        {
            _retryableServerlessConnectivityError = new HashSet<int>
            {
                //// SQL Error Code: 40613
                //// Database XXXX on server YYYY is not currently available. Please retry the connection later. If the problem persists, contact customer 
                //// support, and provide them the session tracing ID of ZZZZZ.
                40613, 
            };
        }

        /// <summary>
        /// Wait for SqlConnection to handle sleeping serverless instances (allows for them to wake up, otherwise it will result in errors).
        /// </summary>
        public static SqlRetryLogicBaseProvider ServerlessDBRetryProvider()
        {
            var serverlessRetryLogic = new SqlRetryLogicOption
            {
                NumberOfTries = _serverlessMaxRetries,
                MaxTimeInterval = TimeSpan.FromSeconds(_serverlessMaxIntervalTime),
                DeltaTime = TimeSpan.FromSeconds(1),
                TransientErrors = _retryableServerlessConnectivityError
            };

            var provider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(serverlessRetryLogic);

            provider.Retrying += (object s, SqlRetryingEventArgs e) =>
            {
                Logger.Information($"attempt {e.RetryCount + 1} - current delay time:{e.Delay}");
                Logger.Information((e.Exceptions[e.Exceptions.Count - 1] is SqlException ex) ? $"{ex.Number}-{ex.Message}" : $"{e.Exceptions[e.Exceptions.Count - 1].Message}");
            };

            return provider;
        }

    }
}