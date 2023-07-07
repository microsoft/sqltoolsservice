﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal class ServerActions : ManagementActionBase
    {
        #region Variables
        private ServerPrototype serverData;
        private ConfigAction configAction;
        #endregion

        #region Constructor
        public ServerActions(
            CDataContainer context,
            ServerPrototype server,
            ConfigAction configAction)
        {
            this.DataContainer = context;
            this.serverData = server;
            this.configAction = configAction;
        }

        #endregion

        /// <summary>
        /// called on background thread by the framework to execute the action
        /// </summary>
        /// <param name="node"></param>
        public override void OnRunNow(object sender)
        {
            this.serverData.SendDataToServer();
        }
    }
}
