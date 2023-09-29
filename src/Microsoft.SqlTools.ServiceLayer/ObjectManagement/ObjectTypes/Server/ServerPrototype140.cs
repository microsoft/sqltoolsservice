//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal class ServerPrototype140 : ServerPrototype130
    {
        public ServerPrototype140(CDataContainer context) : base(context) { }

        public string OperatingSystem
        {
            get
            {
                return this.currentState.OperatingSystem;
            }
            set
            {
                this.currentState.OperatingSystem = value;
            }
        }

        public string Platform
        {
            get
            {
                return this.currentState.Platform;
            }
            set
            {
                this.currentState.Platform = value;
            }
        }

        public override void ApplyInfoToPrototype(ServerInfo serverInfo)
        {
            base.ApplyInfoToPrototype(serverInfo);

            this.OperatingSystem = serverInfo.OperatingSystem;
            this.Platform = serverInfo.Platform;
        }
    }
}