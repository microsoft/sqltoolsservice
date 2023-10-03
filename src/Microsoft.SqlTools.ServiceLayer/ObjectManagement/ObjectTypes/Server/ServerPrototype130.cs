//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal class ServerPrototype130 : ServerPrototype
    {
        public ServerPrototype130(Server server, ServerConnection connection) : base(server, connection) { }

        public bool? IsPolyBaseInstalled
        {
            get
            {
                return this.currentState.IsPolyBaseInstalled;
            }
            set
            {
                this.currentState.IsPolyBaseInstalled = value;
            }
        }

        public override void ApplyInfoToPrototype(ServerInfo serverInfo)
        {
            base.ApplyInfoToPrototype(serverInfo);

            this.IsPolyBaseInstalled = serverInfo.IsPolyBaseInstalled;
        }
    }
}