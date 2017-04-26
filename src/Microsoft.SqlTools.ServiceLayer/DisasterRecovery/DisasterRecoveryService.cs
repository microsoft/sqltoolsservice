//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    public class DisasterRecoveryService
    {
        private static readonly Lazy<DisasterRecoveryService> instance = new Lazy<DisasterRecoveryService>(() => new DisasterRecoveryService());

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal DisasterRecoveryService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static DisasterRecoveryService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost   )
        {
            serviceHost.SetRequestHandler(BackupRequest.Type, HandleBackupRequest);
        }

        /// <summary>
        /// Handles a backup request
        /// </summary>
        internal static async Task HandleBackupRequest(
            BackupParams backupParams,
            RequestContext<BackupResponse> requestContext)
        {
            await requestContext.SendResult(new BackupResponse());
        }
    }
}
