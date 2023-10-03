//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal class ServerPrototype150 : ServerPrototype140
    {
        public ServerPrototype150(Server server, ServerConnection connection) : base(server, connection) { }

        public string? HardwareGeneration
        {
            get
            {
                return this.currentState.HardwareGeneration;
            }
            set
            {
                this.currentState.HardwareGeneration = value;
            }
        }

        public string? ServiceTier
        {
            get
            {
                return this.currentState.ServiceTier;
            }
            set
            {
                this.currentState.ServiceTier = value;
            }
        }

        public int? StorageSpaceUsageInMB
        {
            get
            {
                return this.currentState.StorageSpaceUsageInMB;
            }
            set
            {
                this.currentState.StorageSpaceUsageInMB = value;
            }
        }


        public int? ReservedStorageSizeMB
        {
            get
            {
                return this.currentState.ReservedStorageSizeMB;
            }
            set
            {
                this.currentState.ReservedStorageSizeMB = value;
            }
        }

        public override void ApplyInfoToPrototype(ServerInfo serverInfo)
        {
            base.ApplyInfoToPrototype(serverInfo);

            this.HardwareGeneration = serverInfo.HardwareGeneration ?? string.Empty;
            this.ServiceTier = serverInfo.ServiceTier ?? string.Empty;
            this.ReservedStorageSizeMB = serverInfo.ReservedStorageSizeMB.GetValueOrDefault();
            this.StorageSpaceUsageInMB = serverInfo.StorageSpaceUsageInMB.GetValueOrDefault();
        }
    }
}