//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.AvailabilityGroup.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.Utility;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.AvailabilityGroup
{
    /// <summary>
    /// Main class for Availability Group Service functionality
    /// </summary>
    public sealed class AvailabilityGroupService
    {
        private ConnectionService connectionService = null;
        private static readonly Lazy<AvailabilityGroupService> instance = new Lazy<AvailabilityGroupService>(() => new AvailabilityGroupService());

        /// <summary>
        /// Construct a new AvailabilityService instance with default parameters
        /// </summary>
        public AvailabilityGroupService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static AvailabilityGroupService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }


        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(AvailabilityGroupsRequest.Type, HandleAvailabilityGroupsRequest);
        }

        /// <summary>
        /// Handle request to get availability groups
        /// </summary>
        internal async Task HandleAvailabilityGroupsRequest(AvailabilityGroupsRequestParams parameters, RequestContext<AvailabilityGroupsResult> requestContext)
        {
            try
            {
                var result = new AvailabilityGroupsResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    using (var sqlConnection = ConnectionService.OpenSqlConnection(connInfo))
                    {
                        var serverConnection = new ServerConnection(sqlConnection);

                        Server server = new Server(serverConnection);
                        result.Succeeded = true;
                        result.AvailabilityGroups = server.AvailabilityGroups.OfType<SMO.AvailabilityGroup>().Select(ag => CreateAvailabilityGroupInfo(ag)).ToArray();
                        sqlConnection.Close();
                    }
                }
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        private AvailabilityGroupInfo CreateAvailabilityGroupInfo(SMO.AvailabilityGroup availabilityGroup)
        {
            var ag = new AvailabilityGroupInfo
            {
                Name = availabilityGroup.Name,
                ClusterType = GetEnumDisplayName(availabilityGroup.ClusterTypeWithDefault),
                ClusterTypeValue = (int)availabilityGroup.ClusterTypeWithDefault,
                LocalReplicaRole = GetEnumDisplayName(availabilityGroup.LocalReplicaRole),
                LocalReplicaRoleValue = (int)availabilityGroup.LocalReplicaRole,
                Replicas = availabilityGroup.AvailabilityReplicas.OfType<SMO.AvailabilityReplica>().Select(ar => CreateAvailabilityReplicaInfo(ar)).ToArray(),
                Databases = availabilityGroup.AvailabilityDatabases.OfType<SMO.AvailabilityDatabase>().Select(database => CreateAvailabilityDatabaseInfo(database)).ToArray(),
            };

            ag.IsSupported_BasicAvailabilityGroup = availabilityGroup.IsSupportedProperty("BasicAvailabilityGroup");
            ag.BasicAvailabilityGroup = ag.IsSupported_BasicAvailabilityGroup ? availabilityGroup.BasicAvailabilityGroup : false;

            ag.IsSupported_DatabaseHealthTrigger = availabilityGroup.IsSupportedProperty("DatabaseHealthTrigger");
            ag.DatabaseHealthTrigger = ag.IsSupported_DatabaseHealthTrigger ? availabilityGroup.DatabaseHealthTrigger : false;

            ag.IsSupported_DtcSupportEnabled = availabilityGroup.IsSupportedProperty("DtcSupportEnabled");
            ag.DtcSupportEnabled = ag.IsSupported_DtcSupportEnabled ? availabilityGroup.DtcSupportEnabled : false;

            ag.IsSupported_RequiredSynchronizedSecondariesToCommit = availabilityGroup.IsSupportedProperty("RequiredSynchronizedSecondariesToCommit");
            ag.RequiredSynchronizedSecondariesToCommit = ag.IsSupported_RequiredSynchronizedSecondariesToCommit ? availabilityGroup.RequiredSynchronizedSecondariesToCommit : 0;

            return ag;
        }

        private AvailabilityReplicaInfo CreateAvailabilityReplicaInfo(SMO.AvailabilityReplica replica)
        {
            var ar = new AvailabilityReplicaInfo
            {
                Name = replica.Name,
                Role = GetEnumDisplayName(replica.Role),
                RoleValue = (int)replica.Role,
                AvailabilityMode = GetEnumDisplayName(replica.AvailabilityMode),
                AvailabilityModeValue = (int)replica.AvailabilityMode,
                FailoverMode = GetEnumDisplayName(replica.FailoverMode),
                FailoverModeValue = (int)replica.FailoverMode,
                ConnectionsInPrimaryRole = GetEnumDisplayName(replica.ConnectionModeInPrimaryRole),
                ConnectionsInPrimaryRoleValue = (int)replica.ConnectionModeInPrimaryRole,
                ReadableSecondary = GetEnumDisplayName(replica.ConnectionModeInSecondaryRole),
                ReadableSecondaryValue = (int)replica.ConnectionModeInSecondaryRole,
                SessionTimeoutInSeconds = replica.SessionTimeout,
                EndpointUrl = replica.EndpointUrl,
                State = GetEnumDisplayName(replica.ConnectionState),
                StateValue = (int)replica.ConnectionState
            };

            ar.IsSupported_SeedingMode = replica.IsSeedingModeSupported;
            if (ar.IsSupported_SeedingMode)
            {
                ar.SeedingMode = GetEnumDisplayName(replica.SeedingMode);
                ar.SeedingModeValue = (int)replica.SeedingMode;
            }

            return ar;
        }

        private AvailabilityDatabaseInfo CreateAvailabilityDatabaseInfo(SMO.AvailabilityDatabase database)
        {
            return new AvailabilityDatabaseInfo
            {
                Name = database.Name,
                State = GetEnumDisplayName(database.SynchronizationState),
                StateValue = (int)database.SynchronizationState,
                IsJoined = database.IsJoined,
                IsSuspended = database.IsSuspended,
            };
        }

        private string GetEnumDisplayName<T>(T enumValue)
        {
            TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
            return typeConverter.ConvertToString(enumValue);
        }
    }
}
