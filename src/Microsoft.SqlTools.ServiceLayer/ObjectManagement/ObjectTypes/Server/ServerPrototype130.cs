//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal class ServerPrototype130 : ServerPrototype
    {
        public ServerPrototype130(CDataContainer context) : base(context) { }

        public bool IsPolyBaseInstalled
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