//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    public class AzureAuthenticationProvider : SqlAuthenticationProvider
    {
        private static int count = 0;
        private Task lastTask = null;

        public override async Task<SqlAuthenticationToken> AcquireTokenAsync(SqlAuthenticationParameters parameters)
        {
            if (lastTask != null)
            {
                await lastTask;
            }
            count++;
            Logger.Write(TraceEventType.Information, "Request in!" + count);
            RequestSecurityTokenParams message = new RequestSecurityTokenParams()
            {
                Authority = parameters.Authority,
                Provider = "Azure",
                Resource = parameters.Resource,
                ServerName = parameters.ServerName,
                DatabaseName = parameters.DatabaseName,
                ConnectionId = parameters.ConnectionId.ToString(),
                Scope = count.ToString()
            };

            TimeSpan timeout = TimeSpan.FromSeconds(10);

            try
            {
                var task = ConnectionService.Instance.ServiceHost.SendRequest(SecurityTokenRequest.Type, message, true);
                using (var timeoutCancellationTokenSource = new CancellationTokenSource())
                {
                    lastTask =
                        await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                    if (lastTask == task)
                    {
                        timeoutCancellationTokenSource.Cancel();
                        RequestSecurityTokenResponse response = await task;
                        var expiresOn = DateTimeOffset.FromUnixTimeSeconds(response.Expiration);
                        return new SqlAuthenticationToken(response.Token, expiresOn);
                    }

                    throw new TimeoutException("Azure token request timed out");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteWithCallstack(TraceEventType.Error, ex.Message);
                return null;
            }
            finally
            {
                lastTask = null;
            }
        }

        public override bool IsSupported(SqlAuthenticationMethod authenticationMethod)
        {
            return true;
        }
    }
}